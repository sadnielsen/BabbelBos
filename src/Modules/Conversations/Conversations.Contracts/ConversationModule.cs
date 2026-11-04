namespace Conversations.Contracts;

/// <summary>
/// Public face of the Conversation module. Other modules may only reference this
/// assembly: integration events and public DTOs live here.
/// </summary>
public static class ConversationModule
{
    public const string Name = "Conversations";

    public const string Schema = "conversation";
}
