using BuildingBlocks.Domain;

namespace Conversations.Domain;

public static class ConversationErrors
{
    public static readonly DomainError NotFound = new("Conversation.NotFound", "The conversation does not exist.");

    public static readonly DomainError Closed = new("Conversation.Closed", "The conversation is closed.");

    public static readonly DomainError Full = new("Conversation.Full", "The conversation has reached its maximum number of messages.");

    public static readonly DomainError EmptyMessage = new("Conversation.EmptyMessage", "A message needs text.");

    public static readonly DomainError MessageTooLong = new("Conversation.MessageTooLong", "The message is too long.");

    public static readonly DomainError AwaitingGnomeReply = new("Conversation.AwaitingGnomeReply", "The gnome has not answered the previous message yet.");

    public static readonly DomainError NothingToReplyTo = new("Conversation.NothingToReplyTo", "A gnome reply must follow a participant message.");

    public static readonly DomainError UnsafeGeneratedReply = new(
        "Conversation.UnsafeGeneratedReply",
        "A generated reply is not allowed when a safety check requires a fixed reply.");
}
