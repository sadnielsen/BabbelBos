namespace Conversations.Application.Ports;

/// <summary>
/// Port to a language model. The request and response are our own types, so the
/// application never depends on a provider SDK (see CLAUDE.md, LLM en prompts).
/// </summary>
public interface IChatCompletionService
{
    Task<ChatCompletion> CompleteAsync(ChatRequest request, CancellationToken cancellationToken);
}

public enum ChatTurnRole
{
    Participant,
    Gnome,
}

public sealed record ChatTurn(ChatTurnRole Role, string Text);

/// <param name="SystemPrompt">The filled-in system prompt of the gnome.</param>
/// <param name="Turns">Earlier messages followed by the new participant message, oldest first.</param>
/// <param name="MaxOutputTokens">Upper bound for the reply length.</param>
public sealed record ChatRequest(string SystemPrompt, IReadOnlyList<ChatTurn> Turns, int MaxOutputTokens = 300);

public sealed record ChatCompletion(string Text);
