using Microsoft.EntityFrameworkCore;
using UtnAssistant.API.Models;

namespace UtnAssistant.API.Data;

public static class DatabaseChecker
{
    public static async Task CheckAsync(AppDbContext db)
    {
        var count = await db.SyllabusChunks.CountAsync();
        Console.WriteLine($"Syllabus chunks: {count}");
        Console.WriteLine();

        var chunks = await db.SyllabusChunks
            .AsNoTracking()
            .Take(3)
            .ToListAsync();

        Console.WriteLine($"{"SubjectId",-36} | {"Content Preview",-50} | {"Vector Preview"}");
        Console.WriteLine(new string('-', 120));

        foreach (var chunk in chunks)
        {
            var cleanContent = chunk.Content.Replace("\n", " ").Replace("\r", "");
            var contentPreview = cleanContent.Length > 50
                ? cleanContent.Substring(0, 50)
                : cleanContent;

            var vectorPreview = chunk.Embedding != null
                ? $"[{string.Join(", ", chunk.Embedding.ToArray().Take(3))}...]"
                : "[null]";

            Console.WriteLine($"{chunk.SubjectId,-36} | {contentPreview,-50} | {vectorPreview}");
        }

        Console.WriteLine();
    }
}