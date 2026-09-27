using Microsoft.EntityFrameworkCore;
using UtnAssistant.API.Data;
using UtnAssistant.API.Enums;
using UtnAssistant.API.Models;

namespace UtnAssistant.API.Services;

public record CreateUserRequest(string Email, string Name);
public record UpdateUserRequest(string? Email, string? Name);
public record CreateProgressRequest(string SubjectId, ProgressStatus Status, double? Grade);
public record UpdateProgressDataRequest(ProgressStatus? Status, double? Grade);

public class UsersService
{
    private readonly AppDbContext _context;

    public UsersService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<User> CreateUserAsync(CreateUserRequest request)
    {
        var user = new User
        {
            Id = Guid.NewGuid().ToString(),
            Email = request.Email,
            Name = request.Name
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return user;
    }

    public async Task<List<User>> GetAllUsersAsync()
    {
        return await _context.Users
            .Include(u => u.Progress)
                .ThenInclude(p => p.Subject)
            .ToListAsync();
    }

    public async Task<User?> GetUserByIdAsync(string id)
    {
        return await _context.Users
            .Include(u => u.Progress)
                .ThenInclude(p => p.Subject)
            .FirstOrDefaultAsync(u => u.Id == id);
    }

    public async Task<User?> UpdateUserAsync(string id, UpdateUserRequest request)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null) return null;

        if (request.Email != null) user.Email = request.Email;
        if (request.Name != null) user.Name = request.Name;
        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return user;
    }

    public async Task<bool> DeleteUserAsync(string id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null) return false;

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<UserSubjectProgress?> CreateProgressAsync(string userId, CreateProgressRequest request)
    {
        var userExists = await _context.Users.AnyAsync(u => u.Id == userId);
        if (!userExists) throw new ArgumentException($"User {userId} not found.");

        var subjectExists = await _context.Subjects.AnyAsync(s => s.Id == request.SubjectId);
        if (!subjectExists) throw new ArgumentException($"Subject {request.SubjectId} not found.");

        var existing = await _context.UserSubjectProgresses
            .FirstOrDefaultAsync(p => p.UserId == userId && p.SubjectId == request.SubjectId);

        if (existing != null) throw new InvalidOperationException("Progress already exists.");

        var progress = new UserSubjectProgress
        {
            UserId = userId,
            SubjectId = request.SubjectId,
            Status = request.Status,
            Grade = request.Grade
        };

        _context.UserSubjectProgresses.Add(progress);
        await _context.SaveChangesAsync();

        return await _context.UserSubjectProgresses
            .Include(p => p.Subject)
            .FirstAsync(p => p.Id == progress.Id);
    }

    public async Task<List<UserSubjectProgress>> GetProgressByUserIdAsync(string userId)
    {
        var userExists = await _context.Users.AnyAsync(u => u.Id == userId);
        if (!userExists) throw new ArgumentException("User not found.");

        return await _context.UserSubjectProgresses
            .Where(p => p.UserId == userId)
            .Include(p => p.Subject)
            .ToListAsync();
    }

    public async Task<UserSubjectProgress?> UpdateProgressAsync(string userId, string subjectId, UpdateProgressDataRequest request)
    {
        var progress = await _context.UserSubjectProgresses
            .Include(p => p.Subject)
            .FirstOrDefaultAsync(p => p.UserId == userId && p.SubjectId == subjectId);

        if (progress == null) return null;

        if (request.Status.HasValue) progress.Status = request.Status.Value;
        if (request.Grade.HasValue) progress.Grade = request.Grade.Value;
        progress.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return progress;
    }

    public async Task<bool> DeleteProgressAsync(string userId, string subjectId)
    {
        var progress = await _context.UserSubjectProgresses
            .FirstOrDefaultAsync(p => p.UserId == userId && p.SubjectId == subjectId);

        if (progress == null) return false;

        _context.UserSubjectProgresses.Remove(progress);
        await _context.SaveChangesAsync();
        return true;
    }
}