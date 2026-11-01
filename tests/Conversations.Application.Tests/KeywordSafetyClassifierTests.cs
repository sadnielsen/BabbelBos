using Conversations.Application.Safety;
using Conversations.Domain;

namespace Conversations.Application.Tests;

/// <summary>
/// Never remove a case or lower an expected severity to make this pass (CLAUDE.md, Veiligheid).
/// </summary>
public class KeywordSafetyClassifierTests
{
    private static async Task<SafetyAssessment> Assess(string text) =>
        await new KeywordSafetyClassifier().AssessAsync(text, TestContext.Current.CancellationToken);

    [Theory]
    [InlineData("Ik wil niet meer leven", ConcernCategory.SuicidalIdeation, Severity.High)]
    [InlineData("ik wil dood!", ConcernCategory.SuicidalIdeation, Severity.High)]
    [InlineData("Was ik maar dood", ConcernCategory.SuicidalIdeation, Severity.High)]
    [InlineData("ik wil mezelf pijn doen", ConcernCategory.SelfHarm, Severity.High)]
    [InlineData("Papa slaat mij", ConcernCategory.Abuse, Severity.High)]
    [InlineData("hij doet me pijn", ConcernCategory.Abuse, Severity.High)]
    [InlineData("die meneer zat aan mijn piemel", ConcernCategory.Abuse, Severity.High)]
    [InlineData("Niemand is thuis en ik heb geen eten gehad", ConcernCategory.Neglect, Severity.High)]
    [InlineData("Ik kan niet ademen", ConcernCategory.MedicalEmergency, Severity.Critical)]
    [InlineData("ik heb pilletjes opgegeten", ConcernCategory.MedicalEmergency, Severity.Critical)]
    [InlineData("Ze pesten mij op school", ConcernCategory.Bullying, Severity.Medium)]
    [InlineData("niemand houdt van mij", ConcernCategory.SevereSadness, Severity.Medium)]
    [InlineData("ik ben heel verdrietig", ConcernCategory.SevereSadness, Severity.Low)]
    public async Task Recognises_concerns(string text, ConcernCategory category, Severity severity)
    {
        var result = await Assess(text);

        result.Category.ShouldBe(category);
        result.Severity.ShouldBe(severity);
        result.Source.ShouldBe(AssessmentSource.KeywordRule);
        result.RuleId.ShouldNotBeNullOrWhiteSpace();
    }

    [Theory]
    [InlineData("Ik heb vandaag een tekening gemaakt")]
    [InlineData("Mijn vis is dood")]
    [InlineData("We speelden dat de draak dood ging")]
    [InlineData("Ik stik van het lachen")]
    [InlineData("ik heb een slaapmuts")]
    public async Task Ordinary_talk_is_safe(string text)
    {
        (await Assess(text)).Severity.ShouldBe(Severity.None);
    }

    [Fact]
    public async Task The_most_severe_rule_wins()
    {
        var result = await Assess("ze pesten mij en ik wil niet meer leven");

        result.Category.ShouldBe(ConcernCategory.SuicidalIdeation);
    }

    [Theory]
    [InlineData("Ik WIL dood.")]
    [InlineData("ik   wil...dood")]
    [InlineData("ik wíl dood")]
    public async Task Case_punctuation_and_accents_do_not_matter(string text)
    {
        (await Assess(text)).Severity.ShouldBe(Severity.High);
    }
}
