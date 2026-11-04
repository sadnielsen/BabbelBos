using Conversations.Application.Ports;
using Conversations.Application.Safety;
using Conversations.Domain;

using GnomeCatalog.Contracts;

using Microsoft.Extensions.Logging;

namespace Conversations.Application.Replies;

/// <summary>
/// Decides what the gnome says to the last participant message. The order is fixed:
/// <list type="number">
/// <item>The participant message already has a safety assessment. If it requires a fixed reply, use the safety script. The LLM is not called.</item>
/// <item>Otherwise ask the LLM. If the model itself reports a High/Critical concern, use the safety script.</item>
/// <item>Check the generated text (form and safety). Retry once if it fails.</item>
/// <item>Anything that fails technically ends in the fixed fallback (fail closed).</item>
/// </list>
/// </summary>
public sealed partial class GnomeReplyService(
    IChatCompletionService chat,
    ISafetyClassifier classifier,
    ILogger<GnomeReplyService> logger)
{
    public const int MaxAttempts = 2;

    public async Task<GnomeReply> CreateReplyAsync(
        Conversation conversation,
        Message participantMessage,
        GnomeProfile gnome,
        ParticipantProfile participant,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(conversation);
        ArgumentNullException.ThrowIfNull(participantMessage);
        ArgumentNullException.ThrowIfNull(gnome);

        if (participantMessage.Safety.RequiresFixedReply)
        {
            return FixedReply(participantMessage.Safety, gnome, participant);
        }

        var request = PromptComposer.Compose(gnome, participant, conversation, now);

        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            ChatCompletion completion;
            try
            {
                completion = await chat.CompleteAsync(request, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception exception) when (exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
            {
                LogLlmFailed(logger, exception, conversation.Id.Value);
                return Fallback(gnome, participant);
            }

            var parsed = GnomeReplyParser.TryParse(completion.Text);
            if (parsed is null)
            {
                LogRejected(logger, conversation.Id.Value, attempt, "unreadable");
                continue;
            }

            if (parsed.SelfReport.RequiresFixedReply)
            {
                return FixedReply(parsed.SelfReport, gnome, participant);
            }

            if (!ReplyGuard.IsAcceptable(parsed.Reply))
            {
                LogRejected(logger, conversation.Id.Value, attempt, "form");
                continue;
            }

            var outputSafety = await classifier
                .AssessOrFailClosedAsync(parsed.Reply, logger, cancellationToken)
                .ConfigureAwait(false);

            if (outputSafety.RequiresFixedReply)
            {
                LogRejected(logger, conversation.Id.Value, attempt, "safety");
                continue;
            }

            return new GnomeReply(parsed.Reply, ReplyKind.Generated, outputSafety.MostSevere(parsed.SelfReport));
        }

        return Fallback(gnome, participant);
    }

    private static GnomeReply FixedReply(SafetyAssessment safety, GnomeProfile gnome, ParticipantProfile participant)
    {
        if (safety.HasCheckFailed)
        {
            return Fallback(gnome, participant);
        }

        var category = (safety.Category ?? ConcernCategory.Other).ToString();
        var script = gnome.SafetyScriptFor(category);
        var text = TemplateRenderer.Render(script, TemplateRenderer.ParticipantValues(participant, gnome.Name));

        return new GnomeReply(text, ReplyKind.SafetyScript, safety);
    }

    private static GnomeReply Fallback(GnomeProfile gnome, ParticipantProfile participant)
    {
        var text = TemplateRenderer.Render(gnome.FallbackScript, TemplateRenderer.ParticipantValues(participant, gnome.Name));
        return new GnomeReply(text, ReplyKind.Fallback, SafetyAssessment.CheckFailed());
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "LLM call failed for conversation {ConversationId}; using fallback")]
    private static partial void LogLlmFailed(ILogger logger, Exception exception, Guid conversationId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Generated reply rejected for conversation {ConversationId} (attempt {Attempt}, reason {Reason})")]
    private static partial void LogRejected(ILogger logger, Guid conversationId, int attempt, string reason);
}
