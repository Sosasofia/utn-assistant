using Microsoft.AspNetCore.Mvc;
using UtnAssistant.API.Services;

namespace UtnAssistant.API.Endpoints;

public static class SubjectEndpoints
{
    public static RouteGroupBuilder MapSubjectEndpoints(this IEndpointRouteBuilder routes)
    {
        var subjectsGroup = routes.MapGroup("/api/subjects");
        var careersGroup = routes.MapGroup("/api/careers");
        var usersGroup = routes.MapGroup("/api/users");

        // GET /api/careers/{code}/subjects
        careersGroup.MapGet("/{code}/subjects", async (string code, SubjectsService service) =>
        {
            var subjects = await service.GetSubjectsByCareerCodeAsync(code);
            return Results.Ok(new { subjects, total = subjects.Count });
        });

        // GET /api/subjects/career/{careerId}
        subjectsGroup.MapGet("/career/{careerId}", async (string careerId, SubjectsService service) =>
        {
            var subjects = await service.GetSubjectsByCareerAsync(careerId);
            return Results.Ok(new { subjects, total = subjects.Count });
        });

        // GET /api/subjects/{id}
        subjectsGroup.MapGet("/{id}", async (string id, [FromQuery] bool? includeCorrelatives, SubjectsService service) =>
        {
            var subject = await service.GetSubjectByIdAsync(id, includeCorrelatives ?? false);
            return subject != null ? Results.Ok(subject) : Results.NotFound();
        });

        // GET /api/users/{userId}/can-take/{subjectId}
        usersGroup.MapGet("/{userId}/can-take/{subjectId}", async (string userId, string subjectId, SubjectsService service) =>
        {
            var result = await service.CanTakeSubjectAsync(userId, subjectId);
            return Results.Ok(result);
        });

        // GET /api/users/{userId}/available-subjects/{careerId}
        usersGroup.MapGet("/{userId}/available-subjects/{careerId}", async (string userId, string careerId, SubjectsService service) =>
        {
            var subjects = await service.GetAvailableSubjectsAsync(userId, careerId);
            return Results.Ok(subjects);
        });

        return subjectsGroup;
    }
}