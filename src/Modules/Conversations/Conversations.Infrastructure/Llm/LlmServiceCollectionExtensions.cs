using Conversations.Application.Ports;

using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using OllamaSharp;

namespace Conversations.Infrastructure.Llm;

internal static class LlmServiceCollectionExtensions
{
    public const string TelemetrySourceName = "Babbelbos.Llm";

    public static IServiceCollection AddLlm(this IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection(LlmOptions.SectionName);
        services.Configure<LlmOptions>(section);

        var options = section.Get<LlmOptions>() ?? new LlmOptions();

        switch (options.Provider.ToUpperInvariant())
        {
            case "FAKE":
                services.AddSingleton<IChatCompletionService, FakeChatCompletionService>();
                return services;

            case "OPENAI":
                if (string.IsNullOrWhiteSpace(options.OpenAI.ApiKey))
                {
                    throw new InvalidOperationException(
                        "Llm:Provider is OpenAI but Llm:OpenAI:ApiKey is empty. Set it with dotnet user-secrets (see README).");
                }

                AddChatClient(services, new OpenAI.Chat.ChatClient(options.OpenAI.Model, options.OpenAI.ApiKey).AsIChatClient());
                return services;

            case "OLLAMA":
                AddChatClient(services, new OllamaApiClient(options.Ollama.Endpoint, options.Ollama.Model));
                return services;

            default:
                throw new InvalidOperationException($"Unknown Llm:Provider '{options.Provider}'. Use Fake, OpenAI or Ollama.");
        }
    }

    private static void AddChatClient(IServiceCollection services, IChatClient innerClient)
    {
        // OpenTelemetry without sensitive data: no prompts or replies in traces (see CLAUDE.md, Privacy).
        services.AddSingleton(innerClient
            .AsBuilder()
            .UseOpenTelemetry(sourceName: TelemetrySourceName, configure: telemetry => telemetry.EnableSensitiveData = false)
            .Build());
        services.AddSingleton<IChatCompletionService, ChatClientCompletionService>();
    }
}
