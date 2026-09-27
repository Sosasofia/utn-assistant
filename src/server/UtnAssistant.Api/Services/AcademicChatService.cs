using Microsoft.EntityFrameworkCore;
using OpenAI;
using OpenAI.Chat;
using Pgvector;
using Pgvector.EntityFrameworkCore;
using System.ClientModel;
using UtnAssistant.API.Models;

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

    public async Task<string> AskQuestionAsync(string userMessage)
    {
        var embeddingClient = _client.GetEmbeddingClient(_embeddingModel);
        var embedResponse = await embeddingClient.GenerateEmbeddingAsync(userMessage);
        var queryVector = new Vector(embedResponse.Value.ToFloats().ToArray());

        var matches = await _context.SyllabusChunks
            .Where(c => c.Embedding != null)
            .OrderBy(c => c.Embedding!.CosineDistance(queryVector))
            .Take(3)
            .Include(c => c.Subject)
                .ThenInclude(s => s.CorrelativesAsTarget)
                    .ThenInclude(corr => corr.RequiredSubject)
            .ToListAsync();

        if (matches.Count == 0)
        {
            return "No se encontró información relevante en los planes de estudio disponibles.";
        }

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

        var contextText = string.Join("\n\n", contextBlocks);


        var chatClient = _client.GetChatClient(_chatModel);

        var messages = new List<ChatMessage>
        {
            new SystemChatMessage($"""
                You are an academic assistant for the UTN Information Systems Engineering program. 
                Answer the user's question strictly using the provided curriculum context below. 
                If the answer is not contained in the context, say you do not know. 
                Respond in Spanish if the user's question is in Spanish or if the user's question is in another language, respond in english.
                
                CONTEXTO DEL PLAN DE ESTUDIOS:
                {contextText}
                """),
            new UserChatMessage(userMessage)
        };

        var chatOptions = new ChatCompletionOptions { Temperature = 0.2f };
        var chatResponse = await chatClient.CompleteChatAsync(messages, chatOptions);

        return chatResponse.Value.Content[0].Text;
    }
}