using BuildingBlocks.Domain;

using Conversations.Application.Features;
using Conversations.Application.Ports;

using Wolverine;

namespace Babbelbos.Api.Endpoints;

/// <summary>HTTP endpoints for conversations. Endpoints only call Wolverine (see CLAUDE.md).</summary>
internal static class ConversationEndpoints
{
    public static IEndpointRouteBuilder MapConversationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/conversations").WithTags("Conversations");

        group.MapPost("/", async (IMessageBus bus, CancellationToken cancellationToken) =>
            (await bus.InvokeAsync<Result<ConversationStartedResponse>>(new StartConversation(), cancellationToken))
                .ToHttp(started => Results.Created($"/api/conversations/{started.ConversationId}", started)));

        group.MapPost("/{conversationId:guid}/messages", async (Guid conversationId, SendMessageRequest request, IMessageBus bus, CancellationToken cancellationToken) =>
            (await bus.InvokeAsync<Result<GnomeReplyResponse>>(new SendParticipantMessage(conversationId, request.Text), cancellationToken))
                .ToHttp(Results.Ok));

        group.MapPost("/{conversationId:guid}/close", async (Guid conversationId, IMessageBus bus, CancellationToken cancellationToken) =>
            (await bus.InvokeAsync<Result>(new CloseConversation(conversationId), cancellationToken))
                .ToHttp(Results.NoContent));

        group.MapGet("/{conversationId:guid}", async (Guid conversationId, IMessageBus bus, CancellationToken cancellationToken) =>
            (await bus.InvokeAsync<Result<ConversationDetails>>(new GetConversation(conversationId), cancellationToken))
                .ToHttp(Results.Ok));

        return app;
    }

    internal sealed record SendMessageRequest(string Text);
}
