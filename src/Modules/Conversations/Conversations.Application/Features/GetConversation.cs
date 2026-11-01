using BuildingBlocks.Domain;

using Conversations.Application.Ports;
using Conversations.Domain;

namespace Conversations.Application.Features;

/// <summary>Reads a conversation of the current participant, with all messages.</summary>
public sealed record GetConversation(Guid ConversationId);

public sealed class GetConversationHandler(IConversationQueries queries, IParticipantContext participantContext)
{
    public async Task<Result<ConversationDetails>> Handle(GetConversation query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var details = await queries
            .GetAsync(new ConversationId(query.ConversationId), participantContext.Current.Id, cancellationToken)
            .ConfigureAwait(false);

        return details is null
            ? Result.Failure<ConversationDetails>(ConversationErrors.NotFound)
            : Result.Success(details);
    }
}
