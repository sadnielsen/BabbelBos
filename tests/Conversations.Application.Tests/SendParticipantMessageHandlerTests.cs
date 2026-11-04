using BuildingBlocks.Domain;

using Conversations.Application.Features;
using Conversations.Application.Ports;
using Conversations.Application.Replies;
using Conversations.Application.Safety;
using Conversations.Domain;

using Microsoft.Extensions.Logging.Abstractions;

namespace Conversations.Application.Tests;

/// <summary>
/// The safety order of a reply (CLAUDE.md, Veiligheid): input check before the LLM,
/// output check after it, and fail closed on every technical failure.
/// </summary>
public class SendParticipantMessageHandlerTests
{
    private static readonly TenantId Tenant = new(Guid.NewGuid());
    private static readonly ParticipantProfile Sam = new(new ParticipantId(Guid.NewGuid()), "Sam", ["mama", "juf Anja"]);

    private readonly InMemoryConversationRepository _repository = new();
    private readonly FixedClock _clock = new();
    private readonly ScriptedChat _chat = new();

    private ISafetyClassifier _classifier = new KeywordSafetyClassifier();

    private Guid StartConversation(ParticipantProfile? participant = null)
    {
        var conversation = Conversation.Start(Tenant, (participant ?? Sam).Id, new GnomeId("pim"), _clock.UtcNow);
        _repository.Add(conversation);
        return conversation.Id.Value;
    }

    private Task<Result<GnomeReplyResponse>> Send(Guid conversationId, string text)
    {
        var replies = new GnomeReplyService(_chat, _classifier, NullLogger<GnomeReplyService>.Instance);
        var handler = new SendParticipantMessageHandler(
            _repository,
            new FixedParticipantContext(Sam),
            new StaticGnomeCatalog(),
            _classifier,
            replies,
            _clock,
            NullLogger<SendParticipantMessageHandler>.Instance);

        return handler.Handle(new SendParticipantMessage(conversationId, text), TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Normal_message_gets_a_generated_reply_and_is_saved()
    {
        var id = StartConversation();
        _chat.Answers(ScriptedChat.Json("Wauw, een trein! Welke kleur heeft hij?"));

        var result = await Send(id, "Ik heb een trein getekend");

        result.Value.Reply.ShouldBe("Wauw, een trein! Welke kleur heeft hij?");
        result.Value.ReplyKind.ShouldBe(nameof(ReplyKind.Generated));
        _repository.SaveCount.ShouldBe(1);

        var messages = _repository.Items.Values.Single().Messages;
        messages.Count.ShouldBe(2);
        messages[1].PromptVersion.ShouldBe("pim/test");
    }

    [Fact]
    public async Task High_risk_input_gets_the_safety_script_and_the_llm_is_never_called()
    {
        var id = StartConversation();

        var result = await Send(id, "Ik wil niet meer leven");

        result.Value.ReplyKind.ShouldBe(nameof(ReplyKind.SafetyScript));
        result.Value.Reply.ShouldBe("SCRIPT-SUICIDAL Sam ga naar mama");
        _chat.Requests.ShouldBeEmpty();
    }

    [Fact]
    public async Task Abuse_script_does_not_name_the_primary_trusted_adult()
    {
        var id = StartConversation();

        var result = await Send(id, "Papa slaat mij");

        result.Value.Reply.ShouldBe("SCRIPT-ABUSE Sam ga naar een grote mens");
        result.Value.Reply.ShouldNotContain("mama");
    }

    [Fact]
    public async Task Failing_input_check_fails_closed_without_calling_the_llm()
    {
        _classifier = new ThrowingClassifier();
        var id = StartConversation();

        var result = await Send(id, "Hallo Pim");

        result.Value.ReplyKind.ShouldBe(nameof(ReplyKind.Fallback));
        result.Value.Reply.ShouldBe("SCRIPT-FALLBACK Sam");
        _chat.Requests.ShouldBeEmpty();
    }

    [Fact]
    public async Task Failing_llm_gives_the_fallback()
    {
        var id = StartConversation();
        _chat.Throws(new HttpRequestException("LLM down"));

        var result = await Send(id, "Hallo Pim");

        result.Value.ReplyKind.ShouldBe(nameof(ReplyKind.Fallback));
    }

    [Fact]
    public async Task Llm_timeout_gives_the_fallback()
    {
        var id = StartConversation();
        _chat.Throws(new TaskCanceledException("timeout"));

        var result = await Send(id, "Hallo Pim");

        result.Value.ReplyKind.ShouldBe(nameof(ReplyKind.Fallback));
    }

    [Fact]
    public async Task Llm_self_reported_high_concern_replaces_its_text_with_the_safety_script()
    {
        var id = StartConversation();
        _chat.Answers(ScriptedChat.Json("Vrije tekst", """{"category": "SuicidalIdeation", "severity": "High", "reason": "x"}"""));

        var result = await Send(id, "Ik ben er liever niet");

        result.Value.ReplyKind.ShouldBe(nameof(ReplyKind.SafetyScript));
        result.Value.Reply.ShouldNotContain("Vrije tekst");
    }

    [Fact]
    public async Task Unreadable_llm_answer_is_retried_once()
    {
        var id = StartConversation();
        _chat.Answers("geen json", ScriptedChat.Json("Hoi Sam!"));

        var result = await Send(id, "Hallo Pim");

        result.Value.Reply.ShouldBe("Hoi Sam!");
        _chat.Requests.Count.ShouldBe(2);
    }

    [Fact]
    public async Task Two_unreadable_answers_give_the_fallback()
    {
        var id = StartConversation();
        _chat.Answers("geen json", "nog steeds geen json");

        var result = await Send(id, "Hallo Pim");

        result.Value.ReplyKind.ShouldBe(nameof(ReplyKind.Fallback));
        _chat.Requests.Count.ShouldBe(GnomeReplyService.MaxAttempts);
    }

    [Fact]
    public async Task Unsafe_generated_text_is_never_sent()
    {
        var id = StartConversation();
        _chat.Answers(ScriptedChat.Json("Wil jij ook niet meer leven?"), ScriptedChat.Json("Ik wil dood"));

        var result = await Send(id, "Hallo Pim");

        result.Value.ReplyKind.ShouldBe(nameof(ReplyKind.Fallback));
        result.Value.Reply.ShouldNotContain("dood");
        result.Value.Reply.ShouldNotContain("leven");
    }

    [Fact]
    public async Task Too_long_generated_text_is_rejected()
    {
        var id = StartConversation();
        var longReply = string.Join(' ', Enumerable.Repeat("woord", ReplyGuard.MaxWords + 1));
        _chat.Answers(ScriptedChat.Json(longReply), ScriptedChat.Json("Kort en lief."));

        var result = await Send(id, "Hallo Pim");

        result.Value.Reply.ShouldBe("Kort en lief.");
    }

    [Fact]
    public async Task Conversation_of_another_participant_is_not_found()
    {
        var other = Sam with { Id = new ParticipantId(Guid.NewGuid()) };
        var id = StartConversation(other);

        var result = await Send(id, "Hallo");

        result.Error.ShouldBe(ConversationErrors.NotFound);
        _repository.SaveCount.ShouldBe(0);
    }

    [Fact]
    public async Task Invalid_message_is_not_saved_and_the_llm_is_not_called()
    {
        var id = StartConversation();

        var result = await Send(id, "   ");

        result.Error.ShouldBe(ConversationErrors.EmptyMessage);
        _chat.Requests.ShouldBeEmpty();
        _repository.SaveCount.ShouldBe(0);
    }

    [Fact]
    public async Task The_llm_gets_the_filled_in_system_prompt_and_the_history()
    {
        var id = StartConversation();
        _chat.Answers(ScriptedChat.Json("Hoi!"), ScriptedChat.Json("Leuk!"));

        await Send(id, "Hallo Pim");
        await Send(id, "Ik heb een hond");

        var request = _chat.Requests[^1];
        request.SystemPrompt.ShouldBe("Je bent Pim Fluister. Je praat met Sam. Grote mensen: mama, juf Anja. Geheugen: (nog niets bekend). Het is zaterdagochtend.");
        request.Turns.Select(t => t.Role).ShouldBe([ChatTurnRole.Participant, ChatTurnRole.Gnome, ChatTurnRole.Participant]);
        request.Turns[^1].Text.ShouldBe("Ik heb een hond");
    }
}
