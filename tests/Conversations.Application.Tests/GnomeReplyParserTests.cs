using Conversations.Application.Replies;
using Conversations.Domain;

namespace Conversations.Application.Tests;

public class GnomeReplyParserTests
{
    [Fact]
    public void Reads_reply_without_concern()
    {
        var parsed = GnomeReplyParser.TryParse("""{"reply": "Hoi!", "concern": null}""");

        parsed.ShouldNotBeNull();
        parsed.Reply.ShouldBe("Hoi!");
        parsed.SelfReport.Severity.ShouldBe(Severity.None);
    }

    [Fact]
    public void Reads_a_self_reported_concern()
    {
        var parsed = GnomeReplyParser.TryParse("""{"reply": "x", "concern": {"category": "Bullying", "severity": "Medium", "reason": "r"}}""");

        parsed!.SelfReport.ShouldBe(new SafetyAssessment(Severity.Medium, ConcernCategory.Bullying, AssessmentSource.LlmSelfReport));
    }

    [Fact]
    public void Tolerates_a_code_fence_around_the_json()
    {
        GnomeReplyParser.TryParse("```json\n{\"reply\": \"Hoi!\"}\n```")!.Reply.ShouldBe("Hoi!");
    }

    [Theory]
    [InlineData("")]
    [InlineData("gewoon tekst")]
    [InlineData("{\"reply\": \"\"}")]
    [InlineData("{\"reply\": \"x\", \"concern\": {\"severity\": \"Enorm\"}}")]
    public void Returns_null_for_unusable_answers(string raw)
    {
        GnomeReplyParser.TryParse(raw).ShouldBeNull();
    }
}
