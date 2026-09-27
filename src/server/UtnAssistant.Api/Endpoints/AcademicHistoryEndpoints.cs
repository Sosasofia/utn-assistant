using Microsoft.AspNetCore.Mvc;
using UtnAssistant.API.Enums;
using UtnAssistant.API.Services;

namespace UtnAssistant.API.Endpoints;

public static class AcademicHistoryEndpoints
{
    public record UpdateProgressRequest(ProgressStatus Status);

    public static RouteGroupBuilder MapAcademicHistoryEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/academic-history");

        group.MapGet("/dashboard", async (
            [FromQuery] string userId,
            [FromQuery] string? careerId,
            AcademicHistoryService service) =>
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Results.BadRequest(new { error = "userId is required" });
            }

            var result = await service.GetStudentDashboardAsync(userId, careerId);
            return Results.Ok(result);
        });

        group.MapPut("/{userId}/progress/{subjectId}", async (
            string userId,
            string subjectId,
            [FromBody] UpdateProgressRequest body,
            AcademicHistoryService service) =>
        {
            return await service.ToggleSubjectStatusAsync(userId, subjectId, body.Status);
        });

        group.MapDelete("/{userId}/progress/{subjectId}", async (
            string userId,
            string subjectId,
            AcademicHistoryService service) =>
        {
            return await service.ToggleSubjectStatusAsync(userId, subjectId, ProgressStatus.NOT_ENROLLED);
        });

        group.MapGet("/electives/options", async (
            [FromQuery] string? careerId,
            AcademicHistoryService service) =>
        {
            var electives = await service.GetElectiveOptionsAsync(careerId);
            return Results.Ok(electives);
        });

        return group;
    }
}