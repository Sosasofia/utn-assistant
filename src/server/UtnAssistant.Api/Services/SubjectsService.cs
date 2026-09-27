using Microsoft.EntityFrameworkCore;
using UtnAssistant.API.Domain;
using UtnAssistant.API.Enums;
using UtnAssistant.API.Models;

namespace UtnAssistant.API.Services;
public class SubjectsService
{
    private readonly AppDbContext _context;

    public SubjectsService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<Subject>> GetSubjectsByCareerAsync(string careerId)
    {
        return await _context.Subjects
            .Where(s => s.CareerId == careerId)
            .Include(s => s.CorrelativesAsPrereq)
                .ThenInclude(c => c.Subject)
            .Include(s => s.CorrelativesAsTarget)
                .ThenInclude(c => c.RequiredSubject)
            .ToListAsync();
    }

    public async Task<List<Subject>> GetSubjectsByCareerCodeAsync(string careerCode)
    {
        var career = await _context.Careers.FirstOrDefaultAsync(c => c.Code == careerCode);
        if (career == null) return new List<Subject>();

        return await GetSubjectsByCareerAsync(career.Id);
    }

    public async Task<Subject?> GetSubjectByIdAsync(string id, bool includeCorrelatives = false)
    {
        var query = _context.Subjects.AsQueryable();

        if (includeCorrelatives)
        {
            query = query
                .Include(s => s.CorrelativesAsPrereq)
                    .ThenInclude(c => c.Subject)
                .Include(s => s.CorrelativesAsTarget)
                    .ThenInclude(c => c.RequiredSubject);
        }

        return await query.FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<Subject?> GetSubjectByCodeAsync(string careerId, string code)
    {
        return await _context.Subjects
            .Include(s => s.CorrelativesAsPrereq)
                .ThenInclude(c => c.Subject)
            .Include(s => s.CorrelativesAsTarget)
                .ThenInclude(c => c.RequiredSubject)
            .FirstOrDefaultAsync(s => s.CareerId == careerId && s.Code == code);
    }

    public async Task<object> CanTakeSubjectAsync(string userId, string subjectId)
    {
        var subject = await _context.Subjects
            .Include(s => s.CorrelativesAsTarget)
                .ThenInclude(c => c.RequiredSubject)
            .FirstOrDefaultAsync(s => s.Id == subjectId);

        if (subject == null)
        {
            return new { canTake = false, reason = "Subject not found" };
        }

        if (subject.CorrelativesAsTarget.Count == 0)
        {
            return new { canTake = true };
        }

        var userProgress = await _context.UserSubjectProgresses
            .Where(p => p.UserId == userId)
            .ToDictionaryAsync(p => p.SubjectId, p => p.Status);

        var result = AcademicRuleEngine.Evaluate(subject, userProgress);

        if (!result.CanTake)
        {
            return new
            {
                canTake = false,
                reason = "Missing prerequisites",
                missingPrerequisites = result.MissingPrerequisites
            };
        }

        return new { canTake = true };
    }

    public async Task<List<Subject>> GetAvailableSubjectsAsync(string userId, string careerId)
    {
        var subjects = await _context.Subjects
            .Where(s => s.CareerId == careerId)
            .Include(s => s.CorrelativesAsTarget)
                .ThenInclude(c => c.RequiredSubject)
            .ToListAsync();

        var userProgress = await _context.UserSubjectProgresses
            .Where(p => p.UserId == userId)
            .ToDictionaryAsync(p => p.SubjectId, p => p.Status);

        var availableSubjects = new List<Subject>();

        foreach (var subject in subjects)
        {
            var evaluation = AcademicRuleEngine.Evaluate(subject, userProgress);
            if (evaluation.CanTake && evaluation.Status != ProgressStatus.APPROVED)
            {
                availableSubjects.Add(subject);
            }
        }

        return availableSubjects;
    }
}