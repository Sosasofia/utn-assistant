using Microsoft.AspNetCore.Mvc;
using UtnAssistant.API.Services;

namespace UtnAssistant.API.Endpoints;

public record ChatMessageDto(string Role, string Content);
public record ChatRequest(string UserId, string Message, List<ChatMessageDto>? History);

public static class ChatEndpoints
{

    public static RouteGroupBuilder MapChatEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/chat");

        group.MapPost("/", async (
            [FromBody] ChatRequest req, 
            AcademicChatService chatService) =>
        {
            if (string.IsNullOrWhiteSpace(req.Message) || string.IsNullOrWhiteSpace(req.UserId))
            {
                return Results.BadRequest(new { error = "UserId and Message are required." });
            }

            var answer = await chatService.AskQuestionAsync(
                req.Message, 
                req.UserId,
                req.History);

            return Results.Ok(new { answer });
        });

        return group;
    }
}