namespace Conversation.Domain;

/// <summary>Identity of a conversation between a participant and their gnome.</summary>
public readonly record struct ConversationId(Guid Value)
{
    public static ConversationId New() => new(Guid.CreateVersion7());

    public override string ToString() => Value.ToString();
}
