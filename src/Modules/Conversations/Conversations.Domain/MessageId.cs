namespace Conversations.Domain;

/// <summary>Identity of one message in a conversation.</summary>
public readonly record struct MessageId(Guid Value)
{
    public static MessageId New() => new(Guid.CreateVersion7());

    public override string ToString() => Value.ToString();
}
