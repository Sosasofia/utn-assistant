using Microsoft.AspNetCore.Mvc;
using UtnAssistant.API.Services;

namespace UtnAssistant.API.Endpoints;

public static class ChatEndpoints
{
    public record AskAssistantRequest(string Message);

    public static RouteGroupBuilder MapChatEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/chat");

        group.MapPost("/", async ([FromBody] AskAssistantRequest body, AcademicChatService chatService) =>
        {
            if (string.IsNullOrWhiteSpace(body.Message))
            {
                return Results.BadRequest(new { error = "Message cannot be empty." });
            }

            var answer = await chatService.AskQuestionAsync(body.Message);
            return Results.Ok(new { answer });
        });

        return group;
    }
}