using Conversations.Application.Ports;

using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;

namespace Conversations.Infrastructure.Llm;

/// <summary>
/// Adapter from our <see cref="IChatCompletionService"/> port to any Microsoft.Extensions.AI
/// <see cref="IChatClient"/> (OpenAI, Ollama, Azure OpenAI, ...). Swapping provider is configuration only.
/// </summary>
public sealed class ChatClientCompletionService(IChatClient client, IOptions<LlmOptions> options) : IChatCompletionService
{
    public async Task<ChatCompletion> CompleteAsync(ChatRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var messages = new List<ChatMessage>(request.Turns.Count + 1)
        {
            new(ChatRole.System, request.SystemPrompt),
        };
        messages.AddRange(request.Turns.Select(turn => new ChatMessage(
            turn.Role == ChatTurnRole.Participant ? ChatRole.User : ChatRole.Assistant,
            turn.Text)));

        var chatOptions = new ChatOptions
        {
            MaxOutputTokens = request.MaxOutputTokens,
            Temperature = options.Value.Temperature,
            ResponseFormat = ChatResponseFormat.Json,
        };

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(options.Value.TimeoutSeconds));

        var response = await client.GetResponseAsync(messages, chatOptions, timeout.Token).ConfigureAwait(false);
        return new ChatCompletion(response.Text);
    }
}
