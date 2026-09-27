using Microsoft.EntityFrameworkCore;
using OpenAI;
using OpenAI.Chat;
using Pgvector;
using Pgvector.EntityFrameworkCore;
using System.ClientModel;
using UtnAssistant.API.Domain;
using UtnAssistant.API.Enums;
using UtnAssistant.API.Models;
using UtnAssistant.API.Endpoints;

namespace UtnAssistant.API.Services;

public class AcademicChatService
{
    private readonly AppDbContext _context;
    private readonly OpenAIClient _client;
    private readonly string _chatModel;
    private readonly string _embeddingModel;
    private readonly string _endpoint;

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
        var queryVector = new Vector(embedResponse.Value.ToFloats().ToArray());

        var matches = await _context.SyllabusChunks
            .AsNoTracking()
            .Where(c => c.Embedding != null)
            .OrderBy(c => c.Embedding!.CosineDistance(queryVector))
            .Take(3)
            .Include(c => c.Subject)
                .ThenInclude(s => s.CorrelativesAsTarget)
                    .ThenInclude(corr => corr.RequiredSubject)
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

        var syllabusContext = "No specific syllabus details retrieved for this query.";
        if (matches.Count > 0)
        {
            var contextBlocks = matches.Select(match =>
            {
                var dbSubject = match.Subject;
                var reqText = "Correlativas requeridas: Ninguna.";
                if (dbSubject.CorrelativesAsTarget.Count > 0)
                {
                    var reqNames = string.Join(", ", dbSubject.CorrelativesAsTarget.Select(c => c.RequiredSubject.Name));
                    reqText = $"Correlativas requeridas para cursar: {reqNames}.";
                }
                return $"--- Materia: {dbSubject.Name} ---\n{reqText}\n\nContenido PDF:\n{match.Content}";
            });
            syllabusContext = string.Join("\n\n", contextBlocks);
        }

        var prompt = $"""
                You are an academic assistant for the UTN Information Systems Engineering program. 

                Your primary role is to advise the student based strictly on the provided institutional rules and their personal academic record.
                If the answer is not contained in the context, say you do not know. 
                Respond in Spanish if the user's question is in Spanish or if the user's question is in another language, respond in english.

                ### ELIGIBLE TO COURSE NEXT (Pre-verified by the system)
                {eligibleContext}

                ### BLOCKED SUBJECTS & MISSING REQUIREMENTS
                {blockedContext}

                ### FULL CURRICULUM DEPENDENCY MAP
                {fullPrerequisiteMap}

                ### OFFICIAL SYLLABUS & RULES (RAG CONTEXT)
                {syllabusContext}

                ### STUDENT'S CURRENT ACADEMIC PROGRESS
                {historyContext}

                INSTRUCTIONS:
                - If the user asks about anything unrelated to UTN academics, refuse to answer.
                - The ELIGIBLE and BLOCKED lists are the ABSOLUTE TRUTH for immediate enrollment.
                - If the student asks about a subject in the BLOCKED list, you MUST explicitly state: "No podés cursar [Materia] todavía." Then explain the missing prerequisites.
                - FUTURE PLANNING: Use the FULL CURRICULUM DEPENDENCY MAP to trace prerequisites backward. 
                - EXAM LOGIC: If a prerequisite subject is currently listed as 'ATTENDED' in their progress, tell the student their immediate next step is to pass the final exam (rendir y aprobar el final) for that subject.
                - ABSOLUTELY NEVER use phrases like 'based on the context provided', 'with the information I have', 'in what you passed me', or mention your knowledge base. Act with 100% confidence as the university system.
                - If the answer is truly unknowable, state the immediate requirements clearly without apologizing or explaining your internal mechanics.
                - Be concise, direct, and encouraging.
                """;

        var messages = new List<ChatMessage> { new SystemChatMessage(prompt) };

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