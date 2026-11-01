using BuildingBlocks.Application;
using BuildingBlocks.Domain;

using Conversations.Application.Ports;
using Conversations.Domain;

namespace Conversations.Application.Features;

/// <summary>Ends a conversation ("dag kabouter").</summary>
public sealed record CloseConversation(Guid ConversationId);

public sealed class CloseConversationHandler(
    IConversationRepository conversations,
    IParticipantContext participantContext,
    IClock clock)
{
    public async Task<Result> Handle(CloseConversation command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var conversation = await conversations.GetAsync(new ConversationId(command.ConversationId), cancellationToken).ConfigureAwait(false);
        if (conversation is null || conversation.ParticipantId != participantContext.Current.Id)
        {
            return Result.Failure(ConversationErrors.NotFound);
        }

        var closed = conversation.Close(clock.UtcNow);
        if (closed.IsFailure)
        {
            return closed;
        }

        await conversations.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }
}
