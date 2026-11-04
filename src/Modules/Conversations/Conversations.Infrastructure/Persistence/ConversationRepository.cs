using Conversations.Application.Ports;
using Conversations.Domain;

using Microsoft.EntityFrameworkCore;

namespace Conversations.Infrastructure.Persistence;

public sealed class ConversationRepository(ConversationsDbContext db) : IConversationRepository
{
    public Task<Conversation?> GetAsync(ConversationId id, CancellationToken cancellationToken) =>
        db.Conversations.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public void Add(Conversation conversation) => db.Conversations.Add(conversation);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => db.SaveChangesAsync(cancellationToken);
}
