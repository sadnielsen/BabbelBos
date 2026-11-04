using Conversations.Domain;

namespace Conversations.Domain.Tests;

public class SafetyAssessmentTests
{
    [Theory]
    [InlineData(Severity.None, false)]
    [InlineData(Severity.Low, false)]
    [InlineData(Severity.Medium, false)]
    [InlineData(Severity.High, true)]
    [InlineData(Severity.Critical, true)]
    public void High_and_critical_require_a_fixed_reply(Severity severity, bool expected)
    {
        new SafetyAssessment(severity, ConcernCategory.Other, AssessmentSource.Classifier).RequiresFixedReply.ShouldBe(expected);
    }

    [Fact]
    public void A_failed_check_requires_a_fixed_reply()
    {
        SafetyAssessment.CheckFailed().RequiresFixedReply.ShouldBeTrue();
    }

    [Fact]
    public void MostSevere_picks_the_higher_severity_and_a_failed_check_always_wins()
    {
        var low = new SafetyAssessment(Severity.Low, ConcernCategory.SevereSadness, AssessmentSource.KeywordRule);
        var high = new SafetyAssessment(Severity.High, ConcernCategory.Abuse, AssessmentSource.LlmSelfReport);

        low.MostSevere(high).ShouldBe(high);
        high.MostSevere(low).ShouldBe(high);
        high.MostSevere(SafetyAssessment.CheckFailed()).HasCheckFailed.ShouldBeTrue();
    }
}
