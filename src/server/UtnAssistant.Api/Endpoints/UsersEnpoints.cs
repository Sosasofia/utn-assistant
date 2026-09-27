using Microsoft.AspNetCore.Mvc;
using UtnAssistant.API.Services;

namespace UtnAssistant.API.Endpoints;

public static class UserEndpoints
{
    public static RouteGroupBuilder MapUserEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/users");

        group.MapGet("/", async (UsersService service) =>
        {
            var users = await service.GetAllUsersAsync();
            return Results.Ok(users);
        });

        group.MapPost("/", async ([FromBody] CreateUserRequest request, UsersService service) =>
        {
            var user = await service.CreateUserAsync(request);
            return Results.Created($"/api/users/{user.Id}", user);
        });

        group.MapGet("/{id}", async (string id, UsersService service) =>
        {
            var user = await service.GetUserByIdAsync(id);
            return user != null ? Results.Ok(user) : Results.NotFound();
        });

        group.MapPut("/{id}", async (string id, [FromBody] UpdateUserRequest request, UsersService service) =>
        {
            var user = await service.UpdateUserAsync(id, request);
            return user != null ? Results.Ok(user) : Results.NotFound();
        });

        group.MapDelete("/{id}", async (string id, UsersService service) =>
        {
            var deleted = await service.DeleteUserAsync(id);
            return deleted ? Results.NoContent() : Results.NotFound();
        });

        group.MapPost("/{userId}/progress", async (string userId, [FromBody] CreateProgressRequest request, UsersService service) =>
        {
            try
            {
                var progress = await service.CreateProgressAsync(userId, request);
                return Results.Ok(progress);
            }
            catch (ArgumentException ex) { return Results.NotFound(new { error = ex.Message }); }
            catch (InvalidOperationException ex) { return Results.Conflict(new { error = ex.Message }); }
        });

        group.MapGet("/{userId}/progress", async (string userId, UsersService service) =>
        {
            try
            {
                var progress = await service.GetProgressByUserIdAsync(userId);
                return Results.Ok(progress);
            }
            catch (ArgumentException ex) { return Results.NotFound(new { error = ex.Message }); }
        });

        group.MapPut("/{userId}/progress/{subjectId}", async (string userId, string subjectId, [FromBody] UpdateProgressDataRequest request, UsersService service) =>
        {
            var progress = await service.UpdateProgressAsync(userId, subjectId, request);
            return progress != null ? Results.Ok(progress) : Results.NotFound();
        });

        group.MapDelete("/{userId}/progress/{subjectId}", async (string userId, string subjectId, UsersService service) =>
        {
            var deleted = await service.DeleteProgressAsync(userId, subjectId);
            return deleted ? Results.NoContent() : Results.NotFound();
        });

        return group;
    }
}