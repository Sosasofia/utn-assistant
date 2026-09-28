using Microsoft.Data.SqlTypes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using OpenAI;
using OpenAI.Chat;
using System.ClientModel;
using System.Text;
using UtnAssistant.API.Domain;
using UtnAssistant.API.Endpoints;
using UtnAssistant.API.Enums;
using UtnAssistant.API.Models;
using OpenAIChat = OpenAI.Chat;

namespace UtnAssistant.API.Services;

public class AcademicChatService
{
    private readonly AppDbContext _context;
    private readonly OpenAIClient _client;
    private readonly string _chatModel;
    private readonly string _embeddingModel;
    private readonly string _endpoint;

    private readonly IEmbeddingGenerator<string, Embedding<float>> _embeddingGenerator;

    public AcademicChatService(AppDbContext context, IConfiguration config)
    {
        _context = context;

        _chatModel = config["AzureOpenAI:ChatModel"]
            ?? throw new InvalidOperationException("ChatModel configuration is missing.");
        _embeddingModel = config["AzureOpenAI:EmbeddingModel"]
            ?? throw new InvalidOperationException("EmbeddingModel configuration is missing.");
        _endpoint = config["AzureOpenAi:Endpoint"]
            ?? throw new InvalidOperationException("Endpoint configuration is missing");

        var apiKey = new ApiKeyCredential(
            config["AzureOpenAI:ApiKey"]
                ?? throw new InvalidOperationException("OpenAI API key missing"));
        _client = new OpenAIClient(
            credential: apiKey,
            options: new OpenAIClientOptions()
            {
                Endpoint = new(_endpoint),
            });
    }

    public async Task<string> AskQuestionAsync(string userMessage, string userId, List<ChatMessageDto>? history)
    {
        var embeddingClient = _client.GetEmbeddingClient(_embeddingModel);
        var embedResponse = await embeddingClient.GenerateEmbeddingAsync(userMessage);
        var queryVector = new SqlVector<float>(embedResponse.Value.ToFloats().ToArray());

        var matches = await _context.SyllabusChunks
            .OrderBy(b => EF.Functions.VectorDistance("cosine", b.Embedding!, queryVector))
            .Take(3)
            .Include(c => c.Subject)
                .ThenInclude(s => s.CorrelativesAsTarget)
                    .ThenInclude(corr => corr.RequiredSubject)
            .ToListAsync();

        var careerMatches = await _context.CareerChunks
            .OrderBy(c => EF.Functions.VectorDistance("cosine", c.Embedding!, queryVector))
            .Take(2) 
            .ToListAsync();

        var userProgress = await _context.UserSubjectProgresses
                    .Include(p => p.Subject)
                    .Where(p => p.UserId == userId)
                    .AsNoTracking()
                    .ToListAsync();

        var historyMap = userProgress.ToDictionary(p => p.SubjectId, p => p.Status);
        var allSubjects = await _context.Subjects
            .Include(s => s.CorrelativesAsTarget)
                .ThenInclude(c => c.RequiredSubject)
            .AsNoTracking()
            .ToListAsync();

        var fullPrerequisiteMap = string.Join("\n", allSubjects.Select(s =>
        {
            var reqs = s.CorrelativesAsTarget.Count > 0
                ? string.Join(", ", s.CorrelativesAsTarget.Select(c => c.RequiredSubject.Name))
                : "Ninguna";
            return $"- {s.Name}: Requiere {reqs}";
        }));

        var evaluations = allSubjects.Select(s => new
        {
            Name = s.Name,
            Result = AcademicRuleEngine.Evaluate(s, historyMap)
        }).ToList();

        var eligibleSubjects = evaluations
            .Where(e => e.Result.CanTake && e.Result.Status == ProgressStatus.NOT_ENROLLED)
            .Select(e => $"- {e.Name}");

        var blockedSubjects = evaluations
            .Where(e => !e.Result.CanTake && e.Result.Status == ProgressStatus.NOT_ENROLLED)
            .Select(e =>
            {
                var missing = string.Join(", ", e.Result.MissingPrerequisites
                    .Select(m => $"{m.RequiredSubjectName} ({m.Type})"));
                return $"- {e.Name}: Missing prerequisites -> {missing}";
            });

        var historyContext = userProgress.Count > 0
            ? string.Join("\n", userProgress.Select(p => $"- {p.Subject.Name}: {p.Status}"))
            : "No academic history recorded yet.";

        var eligibleContext = eligibleSubjects.Any() ? string.Join("\n", eligibleSubjects) : "None.";
        var blockedContext = blockedSubjects.Any() ? string.Join("\n", blockedSubjects) : "None.";

        var contextBuilder = new StringBuilder();

        if (careerMatches.Count > 0)
        {
            contextBuilder.AppendLine("--- INFORMACIÓN DE LA CARRERA ---");
            foreach (var match in careerMatches)
            {
                contextBuilder.AppendLine(match.Content);
                contextBuilder.AppendLine();
            }
        }

        if (matches.Count > 0)
        {
            contextBuilder.AppendLine("--- INFORMACIÓN DE MATERIAS ---");
            foreach (var match in matches)
            {
                var dbSubject = match.Subject;
                var reqText = "Correlativas requeridas: Ninguna.";
                if (dbSubject.CorrelativesAsTarget.Count > 0)
                {
                    var reqNames = string.Join(", ", dbSubject.CorrelativesAsTarget.Select(c => c.RequiredSubject.Name));
                    reqText = $"Correlativas requeridas para cursar: {reqNames}.";
                }
                contextBuilder.AppendLine($"--- Materia: {dbSubject.Name} ---\n{reqText}\n\nContenido PDF:\n{match.Content}");
                contextBuilder.AppendLine();
            }
        }
        var syllabusContext = contextBuilder.Length > 0 ? contextBuilder.ToString() : "No specific syllabus or career details retrieved for this query.";
        var prompt = $"""
                You are the official academic advisor for the UTN Information Systems Engineering program. 
                Your role is to guide the student with absolute confidence based strictly on their academic record and institutional rules.

                ### STUDENT ACADEMIC DATA
                - ELIGIBLE TO COURSE NEXT: {eligibleContext}
                - BLOCKED SUBJECTS & MISSING REQUIREMENTS: {blockedContext}
                - STUDENT'S CURRENT PROGRESS: {historyContext}

                ### INSTITUTIONAL KNOWLEDGE
                - FULL CURRICULUM DEPENDENCY MAP: {fullPrerequisiteMap}
                - OFFICIAL SYLLABUS & RULES: {syllabusContext}

                ### CORE DIRECTIVES
                - LANGUAGE: Respond in the language of the user's prompt (default to Spanish for local UTN terminology). 
                - TERMINOLOGY: Translate internal system tags naturally. "ATTENDED" means "Cursada" or "Firmada". "APPROVED" means "Aprobada". NEVER output the English tags to the user.
                - SCOPE: Refuse to answer any questions unrelated to UTN academics or the student's curriculum.
                - THE ABSOLUTE TRUTH: The ELIGIBLE and BLOCKED lists are pre-verified. Rely on them entirely for immediate enrollment answers.
                - EXAM LOGIC: If a required subject is listed as "ATTENDED", the student's immediate next step is to pass the final exam ("rendir y aprobar el final") to unlock its dependents. 
                - FUTURE PLANNING: Use the DEPENDENCY MAP to trace prerequisites backward for long-term planning.

                ### STYLE & TONE (CRITICAL)
                - CONCISENESS: DO NOT list or repeat the student's entire academic history. Go straight to the point: state exactly what they need to do next or why they are blocked.
                - BLOCKED SUBJECTS: If asked about a blocked subject, start directly with: "No podés cursar [Materia] todavía." Then list the exact missing prerequisites.
                - NO META-TALK: NEVER use phrases like "based on the context provided", "according to the system", or "with the information I have". Speak as the definitive university system.
                - IF UNKNOWN: If the answer cannot be determined from the rules, state the requirements clearly without apologizing or explaining your internal mechanics.
                - Be concise, direct, and encouraging.
                """;

        var messages = new List<OpenAIChat.ChatMessage> { new SystemChatMessage(prompt) };

        if (history != null && history.Any())
        {
            var recentHistory = history.TakeLast(3);
            foreach (var msg in recentHistory)
            {
                if (msg.Role.Equals("user", StringComparison.OrdinalIgnoreCase))
                {
                    messages.Add(new UserChatMessage(msg.Content));
                }
                else if (msg.Role.Equals("assistant", StringComparison.OrdinalIgnoreCase))
                {
                    messages.Add(new AssistantChatMessage(msg.Content));
                }
            }
        }

        messages.Add(new UserChatMessage(userMessage));

        var chatClient = _client.GetChatClient(_chatModel);
        var chatOptions = new ChatCompletionOptions { Temperature = 0.2f };
        var chatResponse = await chatClient.CompleteChatAsync(messages, chatOptions);

        return chatResponse.Value.Content[0].Text;
    }
}