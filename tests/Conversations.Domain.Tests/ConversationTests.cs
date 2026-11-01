using BuildingBlocks.Domain;

using Conversations.Domain;

namespace Conversations.Domain.Tests;

public class ConversationTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 1, 10, 0, 0, TimeSpan.Zero);
    private static readonly SafetyAssessment Safe = SafetyAssessment.Safe(AssessmentSource.KeywordRule);
    private static readonly SafetyAssessment High = new(Severity.High, ConcernCategory.SuicidalIdeation, AssessmentSource.KeywordRule);

    private static Conversation NewConversation() =>
        Conversation.Start(new TenantId(Guid.NewGuid()), new ParticipantId(Guid.NewGuid()), new GnomeId("pim"), Now);

    [Fact]
    public void Start_raises_ConversationStarted()
    {
        var conversation = NewConversation();

        conversation.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<ConversationStarted>();
        conversation.IsClosed.ShouldBeFalse();
    }

    [Fact]
    public void Participant_and_gnome_messages_alternate()
    {
        var conversation = NewConversation();

        conversation.AddParticipantMessage("Hallo", Safe, Now).IsSuccess.ShouldBeTrue();
        conversation.AddParticipantMessage("Nog een", Safe, Now).Error.ShouldBe(ConversationErrors.AwaitingGnomeReply);

        conversation.AddGnomeReply("Hoi!", ReplyKind.Generated, Safe, "v1", Now).IsSuccess.ShouldBeTrue();
        conversation.AddGnomeReply("Nog een", ReplyKind.Generated, Safe, "v1", Now).Error.ShouldBe(ConversationErrors.NothingToReplyTo);
    }

    [Fact]
    public void Gnome_cannot_speak_first()
    {
        var conversation = NewConversation();

        conversation.AddGnomeReply("Hoi!", ReplyKind.Generated, Safe, "v1", Now).Error.ShouldBe(ConversationErrors.NothingToReplyTo);
    }

    [Fact]
    public void Conversation_rejects_message_when_closed()
    {
        var conversation = NewConversation();
        conversation.Close(Now).IsSuccess.ShouldBeTrue();

        conversation.AddParticipantMessage("Hallo", Safe, Now).Error.ShouldBe(ConversationErrors.Closed);
        conversation.Close(Now).Error.ShouldBe(ConversationErrors.Closed);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Empty_messages_are_rejected(string text)
    {
        NewConversation().AddParticipantMessage(text, Safe, Now).Error.ShouldBe(ConversationErrors.EmptyMessage);
    }

    [Fact]
    public void Too_long_messages_are_rejected()
    {
        var text = new string('a', Conversation.MaxMessageLength + 1);

        NewConversation().AddParticipantMessage(text, Safe, Now).Error.ShouldBe(ConversationErrors.MessageTooLong);
    }

    [Fact]
    public void Generated_reply_is_refused_after_a_high_concern()
    {
        var conversation = NewConversation();
        conversation.AddParticipantMessage("Ik wil niet meer leven", High, Now);

        conversation.AddGnomeReply("Vrije tekst", ReplyKind.Generated, Safe, "v1", Now)
            .Error.ShouldBe(ConversationErrors.UnsafeGeneratedReply);

        conversation.AddGnomeReply("Vast script", ReplyKind.SafetyScript, High, "v1", Now).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void Generated_reply_is_refused_when_the_input_check_failed()
    {
        var conversation = NewConversation();
        conversation.AddParticipantMessage("Hallo", SafetyAssessment.CheckFailed(), Now);

        conversation.AddGnomeReply("Vrije tekst", ReplyKind.Generated, Safe, "v1", Now)
            .Error.ShouldBe(ConversationErrors.UnsafeGeneratedReply);

        conversation.AddGnomeReply("Fallback", ReplyKind.Fallback, SafetyAssessment.CheckFailed(), "v1", Now).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void Generated_reply_is_refused_when_the_reply_itself_is_unsafe()
    {
        var conversation = NewConversation();
        conversation.AddParticipantMessage("Hallo", Safe, Now);

        conversation.AddGnomeReply("Iets engs", ReplyKind.Generated, High, "v1", Now)
            .Error.ShouldBe(ConversationErrors.UnsafeGeneratedReply);
    }

    [Fact]
    public void Conversation_is_full_after_the_maximum_number_of_messages()
    {
        var conversation = NewConversation();
        for (var i = 0; i < (Conversation.MaxMessages / 2) - 1; i++)
        {
            conversation.AddParticipantMessage("Hallo", Safe, Now).IsSuccess.ShouldBeTrue();
            conversation.AddGnomeReply("Hoi", ReplyKind.Generated, Safe, "v1", Now).IsSuccess.ShouldBeTrue();
        }

        conversation.AddParticipantMessage("Hallo", Safe, Now).IsSuccess.ShouldBeTrue();
        conversation.AddGnomeReply("Hoi", ReplyKind.Generated, Safe, "v1", Now).IsSuccess.ShouldBeTrue();

        conversation.Messages.Count.ShouldBe(Conversation.MaxMessages);
        conversation.AddParticipantMessage("Hallo", Safe, Now).Error.ShouldBe(ConversationErrors.Full);
    }

    [Fact]
    public void Gnome_reply_stores_kind_and_prompt_version()
    {
        var conversation = NewConversation();
        conversation.AddParticipantMessage("Hallo", Safe, Now);

        var reply = conversation.AddGnomeReply("Hoi", ReplyKind.Generated, Safe, "pim/persona.v1", Now).Value;

        reply.Author.ShouldBe(MessageAuthor.Gnome);
        reply.ReplyKind.ShouldBe(ReplyKind.Generated);
        reply.PromptVersion.ShouldBe("pim/persona.v1");
    }
}
