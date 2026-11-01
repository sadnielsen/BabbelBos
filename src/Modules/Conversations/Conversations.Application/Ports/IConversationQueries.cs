using Conversations.Domain;

namespace Conversations.Application.Ports;

/// <summary>Read side: returns DTOs directly, without loading aggregates.</summary>
public interface IConversationQueries
{
    Task<ConversationDetails?> GetAsync(ConversationId id, ParticipantId participantId, CancellationToken cancellationToken);
}

public sealed record ConversationDetails(
    Guid ConversationId,
    string GnomeId,
    DateTimeOffset StartedAt,
    DateTimeOffset? ClosedAt,
    IReadOnlyList<MessageDetails> Messages);

public sealed record MessageDetails(
    Guid MessageId,
    string Author,
    string Text,
    DateTimeOffset SentAt);
