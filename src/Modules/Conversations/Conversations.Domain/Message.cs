using BuildingBlocks.Domain;

namespace Conversations.Domain;

/// <summary>Who wrote a message.</summary>
public enum MessageAuthor
{
    Participant,
    Gnome,
}

/// <summary>How a gnome reply was made. Only <see cref="Generated"/> contains free LLM text.</summary>
public enum ReplyKind
{
    /// <summary>Written by the LLM and passed the output checks.</summary>
    Generated,

    /// <summary>A fixed safety script, because of a High or Critical concern.</summary>
    SafetyScript,

    /// <summary>The fixed fallback, because a check or the LLM failed.</summary>
    Fallback,
}

/// <summary>One message ("fluistering") in a conversation.</summary>
public sealed class Message : Entity<MessageId>
{
    // For EF Core materialisation only.
    private Message()
        : base(default)
    {
        Text = string.Empty;
        Safety = SafetyAssessment.CheckFailed();
    }

    private Message(
        MessageId id,
        MessageAuthor author,
        string text,
        DateTimeOffset sentAt,
        SafetyAssessment safety,
        ReplyKind? replyKind,
        string? promptVersion)
        : base(id)
    {
        Author = author;
        Text = text;
        SentAt = sentAt;
        Safety = safety;
        ReplyKind = replyKind;
        PromptVersion = promptVersion;
    }

    public MessageAuthor Author { get; }

    public string Text { get; }

    public DateTimeOffset SentAt { get; }

    public SafetyAssessment Safety { get; }

    /// <summary>Only set for gnome messages.</summary>
    public ReplyKind? ReplyKind { get; }

    /// <summary>The prompt version used for a gnome message, for traceability.</summary>
    public string? PromptVersion { get; }

    internal static Message FromParticipant(string text, SafetyAssessment safety, DateTimeOffset sentAt) =>
        new(MessageId.New(), MessageAuthor.Participant, text, sentAt, safety, null, null);

    internal static Message FromGnome(
        string text,
        ReplyKind kind,
        SafetyAssessment safety,
        string promptVersion,
        DateTimeOffset sentAt) =>
        new(MessageId.New(), MessageAuthor.Gnome, text, sentAt, safety, kind, promptVersion);
}
