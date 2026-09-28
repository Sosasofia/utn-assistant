using Microsoft.Data.SqlTypes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using OpenAI;
using System.ClientModel;
using System.Text;
using System.Text.RegularExpressions;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;
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
        var filePath = txtPath ?? Path.Combine(AppContext.BaseDirectory, "Data", "Plan-ISI2023.pdf");

        if (!File.Exists(filePath))
        {
            logger.LogError("Text file not found at {FilePath}", filePath);
            return;
        }

        logger.LogInformation("Loading curriculum text...");

        var fullText = ExtractTextFromPdfDocument(filePath);

        IEmbeddingGenerator<string, Embedding<float>> generator = _client
            .GetEmbeddingClient(_embeddingModel)
            .AsIEmbeddingGenerator();

        var career = await _context.Careers.FirstOrDefaultAsync();
        if (career == null)
        {
            logger.LogWarning("No career found in the database.");
            return;
        }

        var sectionsToExtract = new Dictionary<string, string>
        {
            { "Objetivos de la Carrera", @"2\.- OBJETIVOS DE LA CARRERA([\s\S]*?)(?=3\.- PERFIL PROFESIONAL)" },
            { "Perfil Profesional", @"3\.- PERFIL PROFESIONAL([\s\S]*?)(?=4\.- ALCANCES DEL TÍTULO)" },
            { "Alcances del Título", @"4\.- ALCANCES DEL TÍTULO([\s\S]*?)(?=5\.- COMPETENCIAS DE EGRESO)" },
            { "Competencias de Egreso", @"5\.- COMPETENCIAS DE EGRESO([\s\S]*?)(?=6\.- ORGANIZACIÓN DE LA CARRERA)" }
        };

        foreach (var section in sectionsToExtract)
        {
            var match = Regex.Match(fullText, section.Value, RegexOptions.IgnoreCase);
            if (match.Success)
            {
                var sectionContent = match.Groups[1].Value.Trim();

                var structuredChunk = $"""
            Carrera: {career.Name}
            Sección: {section.Key}

            {sectionContent}
            """;

                var embeddingResult = await generator.GenerateVectorAsync(structuredChunk);

                var chunk = new CareerChunk
                {
                    Id = Guid.NewGuid().ToString(),
                    CareerId = career.Id,
                    SectionTitle = section.Key,
                    Content = structuredChunk,
                    Embedding = new SqlVector<float>(embeddingResult)
                };

                _context.CareerChunks.Add(chunk);
                logger.LogInformation("Stored career embedding for: {Section}", section.Key);
            }
        }

        var subjects = await _context.Subjects.OrderBy(s => s.Id).ToListAsync();

        if (subjects.Count == 0)
        {
            logger.LogWarning("No subjects found in the database. Run the database seed first.");
            return;
        }

        logger.LogInformation("Found {Count} subjects to process.", subjects.Count);


        foreach (var subject in subjects)
        {
            logger.LogInformation("Extracting syllabus for: [{Code}] {Name}", subject.Code, subject.Name);

            var blockRegex = new Regex(
                $@"(?:N[°o]\s*de\s*orden:\s*{subject.Code}\b[\s\S]*?)(?=(?:N[°o]\s*de\s*orden:\s*\d+\b)|$)", 
                RegexOptions.IgnoreCase);
            var blockMatch = blockRegex.Match(fullText);

            if (!blockMatch.Success)
            {
                logger.LogWarning("Could not find section for subject [{Code}] {Name}", subject.Code, subject.Name);
                continue;
            }

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

            var embeddingResult = await generator.GenerateVectorAsync(structuredChunk);

            var chunk = new SyllabusChunk
            {
                Id = Guid.NewGuid().ToString(),
                SubjectId = subject.Id,
                Content = structuredChunk,
                Embedding = new SqlVector<float>(embeddingResult)
            };

            _context.SyllabusChunks.Add(chunk);
            await _context.SaveChangesAsync();

            logger.LogInformation("Stored embeddings for: {Name}", subject.Name);
        }

        logger.LogInformation("Syllabus ingestion complete.");
    }

    private string ExtractTextFromPdfDocument(string filePath)
    {
        var document = PdfDocument.Open(filePath);
        var sb = new StringBuilder();

        foreach (Page page in document.GetPages())
        {
            var pageText = ContentOrderTextExtractor.GetText(page);

            if (!string.IsNullOrWhiteSpace(pageText))
            {
                sb.AppendLine(pageText);
            }
        }

        var raw = sb.ToString();

        // Fix line breaks splitting words across lines (e.g. "compu-\r\ntación" -> "computación")
        raw = Regex.Replace(raw, @"(\w+)-\r?\n(\w+)", "$1$2");

        // Normalize multiple blank lines and carriage returns
        raw = Regex.Replace(raw, @"\r\n|\r", "\n");

        return raw;
    }
}