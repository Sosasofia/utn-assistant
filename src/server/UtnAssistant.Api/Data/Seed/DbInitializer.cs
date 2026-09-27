using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using UtnAssistant.API.Enums;
using UtnAssistant.API.Models;

namespace UtnAssistant.API.Data.Seed;

public static class DbInitializer
{
    public static async Task SeedAsync(AppDbContext context, ILogger logger, string? curriculumFilePath = null)
    {
        var filePath = curriculumFilePath ?? Path.Combine(AppContext.BaseDirectory, "Data", "curriculum.json");

        if (!File.Exists(filePath))
        {
            logger.LogError("Curriculum file not found at: {FilePath}", filePath);
            return;
        }

        logger.LogInformation("Loading curriculum from: {FilePath}", filePath);
        var json = await File.ReadAllTextAsync(filePath);
        var curriculum = JsonSerializer.Deserialize<CurriculumRoot>(json);

        if (curriculum == null || string.IsNullOrWhiteSpace(curriculum.Career.Code))
        {
            logger.LogError("Invalid curriculum JSON structure.");
            return;
        }

        var career = await context.Careers.FirstOrDefaultAsync(c => c.Code == curriculum.Career.Code);
        if (career == null)
        {
            career = new Career
            {
                Id = curriculum.Career.Id ?? Guid.NewGuid().ToString(),
                Code = curriculum.Career.Code,
                Name = curriculum.Career.Name,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            context.Careers.Add(career);
        }
        else
        {
            career.Name = curriculum.Career.Name;
            career.UpdatedAt = DateTime.UtcNow;
        }
        await context.SaveChangesAsync();
        logger.LogInformation("Career upserted: {CareerCode}", career.Code);


        var codeToSubject = new Dictionary<string, Subject>();
        foreach (var s in curriculum.Subjects)
        {
            var subject = await context.Subjects
                .FirstOrDefaultAsync(sub => sub.CareerId == career.Id && sub.Code == s.Code);

            if (subject == null)
            {
                subject = new Subject
                {
                    Id = Guid.NewGuid().ToString(),
                    Code = s.Code,
                    Name = s.Name,
                    Year = s.Year,
                    Semester = s.Semester,
                    IsIntegrator = s.IsIntegrator,
                    IsElective = s.IsElective,
                    CareerId = career.Id,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                context.Subjects.Add(subject);
            }
            else
            {
                subject.Name = s.Name;
                subject.Year = s.Year;
                subject.Semester = s.Semester;
                subject.IsIntegrator = s.IsIntegrator;
                subject.IsElective = s.IsElective;
                subject.UpdatedAt = DateTime.UtcNow;
            }

            await context.SaveChangesAsync();
            codeToSubject[s.Code] = subject;
        }
        logger.LogInformation("Core subjects upserted: {Count}", codeToSubject.Count);

        int coreCorrCount = 0;
        foreach (var s in curriculum.Subjects)
        {
            if (!codeToSubject.TryGetValue(s.Code, out var sourceSubject)) continue;

            foreach (var c in s.Correlatives)
            {
                if (!codeToSubject.TryGetValue(c.Code, out var requiredSubject))
                {
                    logger.LogWarning("Missing prerequisite code: {ReqCode} for subject {SubjectCode}", c.Code, s.Code);
                    continue;
                }

                var type = Enum.Parse<CorrelativeType>(c.Type, ignoreCase: true);
                var isTransient = IsCorrelativeTransient(sourceSubject, requiredSubject);

                var existingCorr = await context.Correlatives.FirstOrDefaultAsync(corr =>
                    corr.SubjectId == sourceSubject.Id &&
                    corr.RequiredSubjectId == requiredSubject.Id &&
                    corr.Type == type);

                if (existingCorr == null)
                {
                    context.Correlatives.Add(new Correlative
                    {
                        Id = Guid.NewGuid().ToString(),
                        SubjectId = sourceSubject.Id,
                        RequiredSubjectId = requiredSubject.Id,
                        Type = type,
                        IsTransient = isTransient
                    });
                }
                else
                {
                    existingCorr.IsTransient = isTransient;
                }

                coreCorrCount++;
            }
        }
        await context.SaveChangesAsync();
        logger.LogInformation("Core correlatives upserted: {Count}", coreCorrCount);


        if (curriculum.Electives.Count > 0)
        {
            foreach (var group in curriculum.Electives)
            {
                var resolvedPrereqs = group.Prerequisites
                    .Where(p => codeToSubject.ContainsKey(p.Code))
                    .Select(p => new
                    {
                        RequiredSubjectId = codeToSubject[p.Code].Id,
                        Type = Enum.Parse<CorrelativeType>(p.Type, ignoreCase: true)
                    })
                    .ToList();

                foreach (var el in group.Subjects)
                {
                    var elective = await context.Subjects
                        .FirstOrDefaultAsync(sub => sub.CareerId == career.Id && sub.Code == el.Code);

                    if (elective == null)
                    {
                        elective = new Subject
                        {
                            Id = Guid.NewGuid().ToString(),
                            Code = el.Code,
                            Name = el.Name,
                            Year = el.Year,
                            Semester = 1,
                            IsElective = true,
                            IsIntegrator = false,
                            Credits = 4,
                            CareerId = career.Id,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        };
                        context.Subjects.Add(elective);
                    }
                    else
                    {
                        elective.Name = el.Name;
                        elective.Year = el.Year;
                        elective.IsElective = true;
                        elective.UpdatedAt = DateTime.UtcNow;
                    }
                    await context.SaveChangesAsync();

                    foreach (var req in resolvedPrereqs)
                    {
                        var exists = await context.Correlatives.AnyAsync(corr =>
                            corr.SubjectId == elective.Id &&
                            corr.RequiredSubjectId == req.RequiredSubjectId &&
                            corr.Type == req.Type);

                        if (!exists)
                        {
                            context.Correlatives.Add(new Correlative
                            {
                                Id = Guid.NewGuid().ToString(),
                                SubjectId = elective.Id,
                                RequiredSubjectId = req.RequiredSubjectId,
                                Type = req.Type,
                                IsTransient = false
                            });
                        }
                    }
                }
            }
            await context.SaveChangesAsync();
            logger.LogInformation("Elective groups seeded successfully.");
        }
    }

    private static bool IsCorrelativeTransient(Subject target, Subject required)
    {
        if (required.Year == 1) return false;
        if (target.Year - required.Year > 2) return true;
        if (target.Year == required.Year && required.Semester < target.Semester) return false;
        if (required.Year == 2 && target.Year >= 4) return true;

        return false;
    }
}