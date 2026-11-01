using BuildingBlocks.Domain;

namespace Conversations.Domain;

/// <summary>
/// A conversation between one participant and their gnome. Messages alternate:
/// participant, gnome, participant, gnome. A generated (LLM) reply is only allowed
/// when every safety check on both messages passed.
/// </summary>
public sealed class Conversation : AggregateRoot<ConversationId>
{
    public const int MaxMessages = 100;
    public const int MaxMessageLength = 500;

    private readonly List<Message> _messages = [];

    private Conversation(ConversationId id, TenantId tenantId, ParticipantId participantId, GnomeId gnomeId, DateTimeOffset startedAt)
        : base(id)
    {
        TenantId = tenantId;
        ParticipantId = participantId;
        GnomeId = gnomeId;
        StartedAt = startedAt;
    }

    public TenantId TenantId { get; }

    public ParticipantId ParticipantId { get; }

    public GnomeId GnomeId { get; }

    public DateTimeOffset StartedAt { get; }

    public DateTimeOffset? ClosedAt { get; private set; }

    public bool IsClosed => ClosedAt is not null;

    public IReadOnlyList<Message> Messages => _messages.AsReadOnly();

    public static Conversation Start(TenantId tenantId, ParticipantId participantId, GnomeId gnomeId, DateTimeOffset now)
    {
        var conversation = new Conversation(ConversationId.New(), tenantId, participantId, gnomeId, now);
        conversation.Raise(new ConversationStarted(conversation.Id, tenantId, participantId, gnomeId, now));
        return conversation;
    }

    public Result<Message> AddParticipantMessage(string text, SafetyAssessment safety, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(safety);

        if (IsClosed)
        {
            return Result.Failure<Message>(ConversationErrors.Closed);
        }

        if (string.IsNullOrWhiteSpace(text))
        {
            return Result.Failure<Message>(ConversationErrors.EmptyMessage);
        }

        if (text.Length > MaxMessageLength)
        {
            return Result.Failure<Message>(ConversationErrors.MessageTooLong);
        }

        if (_messages.Count >= MaxMessages - 1)
        {
            return Result.Failure<Message>(ConversationErrors.Full);
        }

        if (_messages.Count > 0 && _messages[^1].Author == MessageAuthor.Participant)
        {
            return Result.Failure<Message>(ConversationErrors.AwaitingGnomeReply);
        }

        var message = Message.FromParticipant(text.Trim(), safety, now);
        _messages.Add(message);
        Raise(new ParticipantMessageReceived(Id, TenantId, ParticipantId, message.Id, safety, now));
        return Result.Success(message);
    }

    public Result<Message> AddGnomeReply(
        string text,
        ReplyKind kind,
        SafetyAssessment replySafety,
        string promptVersion,
        DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(replySafety);

        if (IsClosed)
        {
            return Result.Failure<Message>(ConversationErrors.Closed);
        }

        if (_messages.Count == 0 || _messages[^1].Author != MessageAuthor.Participant)
        {
            return Result.Failure<Message>(ConversationErrors.NothingToReplyTo);
        }

        if (string.IsNullOrWhiteSpace(text))
        {
            return Result.Failure<Message>(ConversationErrors.EmptyMessage);
        }

        var participantSafety = _messages[^1].Safety;
        if (kind == ReplyKind.Generated && (participantSafety.RequiresFixedReply || replySafety.RequiresFixedReply))
        {
            return Result.Failure<Message>(ConversationErrors.UnsafeGeneratedReply);
        }

        var message = Message.FromGnome(text.Trim(), kind, replySafety, promptVersion, now);
        _messages.Add(message);
        Raise(new GnomeReplied(Id, message.Id, kind, now));
        return Result.Success(message);
    }

    public Result Close(DateTimeOffset now)
    {
        if (IsClosed)
        {
            return Result.Failure(ConversationErrors.Closed);
        }

        ClosedAt = now;
        Raise(new ConversationClosed(Id, now));
        return Result.Success();
    }
}
