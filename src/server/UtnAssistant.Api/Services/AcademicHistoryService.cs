using Microsoft.EntityFrameworkCore;
using UtnAssistant.API.Domain;
using UtnAssistant.API.Enums;
using UtnAssistant.API.Models;

namespace UtnAssistant.API.Services;

public class AcademicHistoryService
{
    private readonly AppDbContext _context;

    public AcademicHistoryService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<object> GetStudentDashboardAsync(string userId, string? careerId)
    {
        var resolvedCareerId = await ResolveCareerIdAsync(careerId);

        var allSubjects = await _context.Subjects
            .Where(s => s.CareerId == resolvedCareerId)
            .Include(s => s.CorrelativesAsTarget)
                .ThenInclude(c => c.RequiredSubject)
            .OrderBy(s => s.Year)
            .ThenBy(s => s.Code)
            .ToListAsync();

        var userProgress = await _context.UserSubjectProgresses
            .Where(p => p.UserId == userId)
            .ToDictionaryAsync(p => p.SubjectId, p => p.Status);

        return allSubjects.Select(subject =>
        {
            var evaluation = AcademicRuleEngine.Evaluate(subject, userProgress);

            return new
            {
                subject.Id,
                subject.Code,
                subject.Name,
                subject.Year,
                subject.Semester,
                subject.Credits,
                subject.IsIntegrator,
                subject.IsElective,
                evaluation.Status,
                evaluation.CanTake,
                evaluation.MissingPrerequisites,
                CorrelativesAsTarget = subject.CorrelativesAsTarget
                    .Select(rule => new
                    {
                        rule.Id,
                        rule.Type,
                        rule.IsTransient,
                        RequiredSubject = new
                        {
                            rule.RequiredSubject.Id,
                            rule.RequiredSubject.Code,
                            rule.RequiredSubject.Name
                        }
                    })
                    .ToList()
            };
        });
    }

    public async Task<IResult> ToggleSubjectStatusAsync(string userId, string subjectId, ProgressStatus? status)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null)
        {
            user = new User
            {
                Id = userId,
                Email = $"guest-{userId}@example.com"
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
        }

        var effectiveStatus = status ?? ProgressStatus.NOT_ENROLLED;

        if (effectiveStatus == ProgressStatus.NOT_ENROLLED)
        {
            var existing = await _context.UserSubjectProgresses
                .FirstOrDefaultAsync(p => p.UserId == userId && p.SubjectId == subjectId);

            if (existing != null)
            {
                _context.UserSubjectProgresses.Remove(existing);
                await _context.SaveChangesAsync();
            }

            return Results.Ok(new { message = "Progress removed" });
        }

        var progress = await _context.UserSubjectProgresses
            .FirstOrDefaultAsync(p => p.UserId == userId && p.SubjectId == subjectId);

        if (progress == null)
        {
            progress = new UserSubjectProgress
            {
                UserId = userId,
                SubjectId = subjectId,
                Status = effectiveStatus,
                UpdatedAt = DateTime.UtcNow
            };
            _context.UserSubjectProgresses.Add(progress);
        }
        else
        {
            progress.Status = effectiveStatus;
            progress.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        return Results.Ok(new { message = "Progress saved", status = effectiveStatus });
    }

    public async Task<List<Subject>> GetElectiveOptionsAsync(string? careerId)
    {
        var resolvedCareerId = await ResolveCareerIdAsync(careerId);

        return await _context.Subjects
            .Where(s => s.CareerId == resolvedCareerId &&
                        s.IsElective &&
                        !s.Name.StartsWith("Electiva"))
            .OrderBy(s => s.Name)
            .ToListAsync();
    }

    private async Task<string> ResolveCareerIdAsync(string? careerId)
    {
        if (!string.IsNullOrWhiteSpace(careerId)) return careerId;

        var careers = await _context.Careers.OrderBy(c => c.Code).Select(c => c.Id).ToListAsync();

        if (careers.Count == 1) return careers[0];

        throw new BadHttpRequestException("careerId is required when multiple careers are available");
    }
}