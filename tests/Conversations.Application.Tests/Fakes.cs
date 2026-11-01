using BuildingBlocks.Application;

using Conversations.Application.Ports;
using Conversations.Domain;

using GnomeCatalog.Contracts;

namespace Conversations.Application.Tests;

internal sealed class InMemoryConversationRepository : IConversationRepository
{
    public Dictionary<ConversationId, Conversation> Items { get; } = [];

    public int SaveCount { get; private set; }

    public Task<Conversation?> GetAsync(ConversationId id, CancellationToken cancellationToken) =>
        Task.FromResult(Items.GetValueOrDefault(id));

    public void Add(Conversation conversation) => Items[conversation.Id] = conversation;

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        SaveCount++;
        return Task.CompletedTask;
    }
}

internal sealed class FixedParticipantContext(ParticipantProfile profile) : IParticipantContext
{
    public ParticipantProfile Current { get; } = profile;
}

internal sealed class FixedClock : IClock
{
    public DateTimeOffset UtcNow { get; set; } = new(2026, 11, 7, 9, 30, 0, TimeSpan.Zero);
}

internal sealed class StaticGnomeCatalog : IGnomeCatalog
{
    public static readonly GnomeProfile Pim = new(
        "pim",
        "Pim Fluister",
        "Hallo {{participant_name}}!",
        "Je bent {{gnome_name}}. Je praat met {{participant_name}}. Grote mensen: {{trusted_adults}}. Geheugen: {{memory_summary}}. Het is {{local_time_description}}.",
        "pim/test",
        new Dictionary<string, string>
        {
            ["SuicidalIdeation"] = "SCRIPT-SUICIDAL {{participant_name}} ga naar {{primary_trusted_adult}}",
            ["Abuse"] = "SCRIPT-ABUSE {{participant_name}} ga naar een grote mens",
            ["Default"] = "SCRIPT-DEFAULT {{participant_name}}",
            ["Fallback"] = "SCRIPT-FALLBACK {{participant_name}}",
        });

    public GnomeProfile GetDefault() => Pim;

    public GnomeProfile? Find(string gnomeId) => gnomeId == Pim.Id ? Pim : null;
}

/// <summary>A scripted LLM: returns the queued answers in order, or throws a queued exception.</summary>
internal sealed class ScriptedChat : IChatCompletionService
{
    private readonly Queue<Func<ChatCompletion>> _answers = new();

    public List<ChatRequest> Requests { get; } = [];

    public ScriptedChat Answers(params string[] texts)
    {
        foreach (var text in texts)
        {
            _answers.Enqueue(() => new ChatCompletion(text));
        }

        return this;
    }

    public ScriptedChat Throws(Exception exception)
    {
        _answers.Enqueue(() => throw exception);
        return this;
    }

    public Task<ChatCompletion> CompleteAsync(ChatRequest request, CancellationToken cancellationToken)
    {
        Requests.Add(request);
        return _answers.Count > 0
            ? Task.FromResult(_answers.Dequeue()())
            : throw new InvalidOperationException("The test did not expect another LLM call.");
    }

    public static string Json(string reply, string? concern = null) =>
        concern is null
            ? $$"""{"reply": "{{reply}}", "concern": null}"""
            : $$"""{"reply": "{{reply}}", "concern": {{concern}}}""";
}

internal sealed class ThrowingClassifier : ISafetyClassifier
{
    public Task<SafetyAssessment> AssessAsync(string text, CancellationToken cancellationToken) =>
        throw new HttpRequestException("classifier unavailable");
}
