namespace Conversations.Infrastructure.Llm;

/// <summary>
/// Which language model to use. Configured per environment, for example in user-secrets:
/// <c>Llm:Provider = OpenAI</c> and <c>Llm:OpenAI:ApiKey = sk-...</c>.
/// </summary>
public sealed class LlmOptions
{
    public const string SectionName = "Llm";

    /// <summary><c>Fake</c> (default, no network), <c>OpenAI</c> or <c>Ollama</c>.</summary>
    public string Provider { get; set; } = "Fake";

    /// <summary>Upper bound for one LLM call; after this the gnome gives the fallback reply.</summary>
    public int TimeoutSeconds { get; set; } = 20;

    public float Temperature { get; set; } = 0.7f;

    public OpenAIOptions OpenAI { get; set; } = new();

    public OllamaOptions Ollama { get; set; } = new();
}

public sealed class OpenAIOptions
{
    public string? ApiKey { get; set; }

    public string Model { get; set; } = "gpt-4.1-mini";
}

public sealed class OllamaOptions
{
    public Uri Endpoint { get; set; } = new("http://localhost:11434");

    public string Model { get; set; } = "qwen2.5:7b";
}
