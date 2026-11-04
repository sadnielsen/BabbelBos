using BuildingBlocks.Application;
using BuildingBlocks.Domain;

using Conversations.Application.Ports;
using Conversations.Application.Replies;
using Conversations.Application.Safety;
using Conversations.Domain;

using GnomeCatalog.Contracts;

using Microsoft.Extensions.Logging;

namespace Conversations.Application.Features;

/// <summary>The participant says something; the gnome answers.</summary>
public sealed record SendParticipantMessage(Guid ConversationId, string Text);

public sealed record GnomeReplyResponse(Guid ConversationId, Guid MessageId, string Reply, string ReplyKind);

public sealed partial class SendParticipantMessageHandler(
    IConversationRepository conversations,
    IParticipantContext participantContext,
    IGnomeCatalog gnomes,
    ISafetyClassifier classifier,
    GnomeReplyService replies,
    IClock clock,
    ILogger<SendParticipantMessageHandler> logger)
{
    public async Task<Result<GnomeReplyResponse>> Handle(SendParticipantMessage command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var participant = participantContext.Current;

        // The repository only returns conversations of the current tenant.
        var conversation = await conversations.GetAsync(new ConversationId(command.ConversationId), cancellationToken).ConfigureAwait(false);
        if (conversation is null || conversation.ParticipantId != participant.Id)
        {
            return Result.Failure<GnomeReplyResponse>(ConversationErrors.NotFound);
        }

        var gnome = gnomes.Find(conversation.GnomeId.Value) ?? gnomes.GetDefault();

        // 1. Safety check on the input, before anything else. Fails closed.
        var inputSafety = await classifier.AssessOrFailClosedAsync(command.Text ?? string.Empty, logger, cancellationToken).ConfigureAwait(false);

        var added = conversation.AddParticipantMessage(command.Text ?? string.Empty, inputSafety, clock.UtcNow);
        if (added.IsFailure)
        {
            return Result.Failure<GnomeReplyResponse>(added.Error);
        }

        if (inputSafety.Severity > Severity.None || inputSafety.HasCheckFailed)
        {
            LogConcern(logger, conversation.Id.Value, added.Value.Id.Value, inputSafety.Severity, inputSafety.Category, inputSafety.Source);
        }

        // 2. The reply: fixed script, generated text or fallback.
        var reply = await replies.CreateReplyAsync(conversation, added.Value, gnome, participant, clock.UtcNow, cancellationToken).ConfigureAwait(false);

        var gnomeMessage = conversation.AddGnomeReply(reply.Text, reply.Kind, reply.Safety, gnome.PromptVersion, clock.UtcNow);
        if (gnomeMessage.IsFailure)
        {
            // The domain refused the reply (for example an unsafe generated reply). Never send it.
            return Result.Failure<GnomeReplyResponse>(gnomeMessage.Error);
        }

        await conversations.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success(new GnomeReplyResponse(
            conversation.Id.Value,
            gnomeMessage.Value.Id.Value,
            gnomeMessage.Value.Text,
            reply.Kind.ToString()));
    }

    // Ids and outcome only: never the text (see CLAUDE.md, Privacy). Concerns become alerts in iteration 3.
    [LoggerMessage(Level = LogLevel.Warning, Message = "Concern in conversation {ConversationId}, message {MessageId}: severity {Severity}, category {Category}, source {Source}")]
    private static partial void LogConcern(ILogger logger, Guid conversationId, Guid messageId, Severity severity, ConcernCategory? category, AssessmentSource source);
}
