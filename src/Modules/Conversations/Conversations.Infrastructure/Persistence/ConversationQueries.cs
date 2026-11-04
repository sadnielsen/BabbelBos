using Conversations.Application.Ports;
using Conversations.Domain;

using Microsoft.EntityFrameworkCore;

namespace Conversations.Infrastructure.Persistence;

/// <summary>Read side: projects straight to DTOs, without tracking.</summary>
public sealed class ConversationQueries(ConversationsDbContext db) : IConversationQueries
{
    public async Task<ConversationDetails?> GetAsync(ConversationId id, ParticipantId participantId, CancellationToken cancellationToken)
    {
        var conversation = await db.Conversations
            .AsNoTracking()
            .Where(c => c.Id == id && c.ParticipantId == participantId)
            .Select(c => new
            {
                c.Id,
                c.GnomeId,
                c.StartedAt,
                c.ClosedAt,
                Messages = c.Messages
                    .OrderBy(m => m.SentAt)
                    .Select(m => new { m.Id, m.Author, m.Text, m.SentAt })
                    .ToList(),
            })
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        return conversation is null
            ? null
            : new ConversationDetails(
                conversation.Id.Value,
                conversation.GnomeId.Value,
                conversation.StartedAt,
                conversation.ClosedAt,
                conversation.Messages
                    .Select(m => new MessageDetails(m.Id.Value, m.Author.ToString(), m.Text, m.SentAt))
                    .ToList());
    }
}
