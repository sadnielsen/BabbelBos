using Conversations.Domain;

namespace Conversations.Application.Ports;

/// <summary>Write side: loads and saves the <see cref="Conversation"/> aggregate within the current tenant.</summary>
public interface IConversationRepository
{
    Task<Conversation?> GetAsync(ConversationId id, CancellationToken cancellationToken);

    void Add(Conversation conversation);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
