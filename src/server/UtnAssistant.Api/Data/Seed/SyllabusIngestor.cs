using Microsoft.EntityFrameworkCore;
using OpenAI;
using Pgvector;
using System.ClientModel;
using System.Text.RegularExpressions;
using UtnAssistant.API.Models;

namespace UtnAssistant.API.Data.Seed;

public class SyllabusIngestor
{
    private readonly AppDbContext _context;
    private readonly OpenAIClient _client;
    private readonly string _embeddingModel;
    private readonly string _endpoint;

    public SyllabusIngestor(AppDbContext context, IConfiguration config)
    {
        _context = context;
        _embeddingModel = config["AzureOpenAI:EmbeddingModel"]
            ?? throw new InvalidOperationException("EmbeddingModel config missing");

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

    public async Task IngestAsync(ILogger logger, string? txtPath = null)
    {
        var filePath = txtPath ?? Path.Combine(AppContext.BaseDirectory, "Data", "plan.txt");

        if (!File.Exists(filePath))
        {
            logger.LogError("Text file not found at {FilePath}", filePath);
            return;
        }

        logger.LogInformation("Loading curriculum text...");
        var fullText = await File.ReadAllTextAsync(filePath);
        var subjects = await _context.Subjects.OrderBy(s => s.Id).ToListAsync();

        if (subjects.Count == 0)
        {
            logger.LogWarning("No subjects found in the database. Run the database seed first.");
            return;
        }

        logger.LogInformation("Found {Count} subjects to process.", subjects.Count);

        var embeddingClient = _client.GetEmbeddingClient(_embeddingModel);

        foreach (var subject in subjects)
        {
            logger.LogInformation("Extracting syllabus for: [{Code}] {Name}", subject.Code, subject.Name);

            var blockRegex = new Regex($@"(?:N[°o]\s*de\s*orden:\s*{subject.Code}\b[\s\S]*?)(?=(?:N[°o]\s*de\s*orden:\s*\d+\b)|$)", RegexOptions.IgnoreCase);
            var blockMatch = blockRegex.Match(fullText);

            if (!blockMatch.Success) continue;

            var blockText = blockMatch.Value;
            var objectivesMatch = Regex.Match(blockText, @"Objetivos\s*([\s\S]*?)(?=Contenidos mínimos|$)", RegexOptions.IgnoreCase);
            var objectives = objectivesMatch.Success ? objectivesMatch.Groups[1].Value.Trim() : "";

            var contentsMatch = Regex.Match(blockText, @"Contenidos mínimos\s*([\s\S]*?)$", RegexOptions.IgnoreCase);
            var minimumContents = contentsMatch.Success ? contentsMatch.Groups[1].Value.Trim() : "";

            var structuredChunk = $"""
                Materia: {subject.Name} (Código: {subject.Code})
                Año / Nivel: {subject.Year}

                Objetivos:
                {objectives}

                Contenidos mínimos:
                {minimumContents}
                """;

            var response = await embeddingClient.GenerateEmbeddingAsync(structuredChunk);
            var vector = response.Value.ToFloats().ToArray();

            var chunk = new SyllabusChunk
            {
                Id = Guid.NewGuid().ToString(),
                SubjectId = subject.Id,
                Content = structuredChunk,
                Embedding = new Vector(vector)
            };

            _context.SyllabusChunks.Add(chunk);
            await _context.SaveChangesAsync();

            logger.LogInformation("Stored embeddings for: {Name}", subject.Name);
        }

        logger.LogInformation("Syllabus ingestion complete.");
    }
}