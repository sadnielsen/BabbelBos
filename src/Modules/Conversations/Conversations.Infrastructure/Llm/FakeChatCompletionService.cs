using Conversations.Application.Ports;

namespace Conversations.Infrastructure.Llm;

/// <summary>
/// Deterministic stand-in for a language model: no network, no costs. Used in tests and as the
/// default when no provider is configured. Always answers with the same friendly question.
/// </summary>
public sealed class FakeChatCompletionService : IChatCompletionService
{
    public const string Reply = "Wat leuk dat je dat vertelt! Wil je er nog meer over vertellen?";

    public Task<ChatCompletion> CompleteAsync(ChatRequest request, CancellationToken cancellationToken) =>
        Task.FromResult(new ChatCompletion($$"""{"reply": "{{Reply}}", "concern": null}"""));
}
