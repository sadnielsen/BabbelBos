using Microsoft.Extensions.DependencyInjection;

namespace Conversation.Infrastructure;

/// <summary>Registers the Conversation module in the host.</summary>
public static class ConversationModuleExtensions
{
    public static IServiceCollection AddConversationModule(this IServiceCollection services)
    {
        // Persistence, LLM adapters and repositories are registered here in later iterations.
        return services;
    }
}
