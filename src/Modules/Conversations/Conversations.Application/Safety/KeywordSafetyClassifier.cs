using System.Globalization;
using System.Text;

using Conversations.Application.Ports;
using Conversations.Domain;

namespace Conversations.Application.Safety;

/// <summary>
/// Deterministic first line of defence: Dutch phrases as young children say them.
/// Fast, predictable and testable. Iteration 3 adds a classifier and moves this into the
/// Safeguarding module behind the same <see cref="ISafetyClassifier"/> port.
/// Never remove phrases or lower severities to make a test pass (see CLAUDE.md).
/// </summary>
public sealed class KeywordSafetyClassifier : ISafetyClassifier
{
    private static readonly SafetyRule[] Rules =
    [
        new("medical.breathing", ConcernCategory.MedicalEmergency, Severity.Critical,
            ["kan niet ademen", "krijg geen lucht", "kan geen lucht"]),
        new("medical.swallowed", ConcernCategory.MedicalEmergency, Severity.Critical,
            ["pillen opgegeten", "pilletjes opgegeten", "medicijnen opgegeten", "heb iets ingeslikt", "iets doorgeslikt",
             "pillen ingenomen", "schoonmaakmiddel gedronken"]),
        new("medical.bleeding", ConcernCategory.MedicalEmergency, Severity.Critical,
            ["heel veel bloed", "veel bloed", "ik bloed heel erg"]),

        new("suicidal.wish", ConcernCategory.SuicidalIdeation, Severity.High,
            ["niet meer leven", "wil dood", "dood willen", "wil doodgaan", "wil dood gaan", "wil dood zijn",
             "wou dat ik dood was", "was ik maar dood", "zelfmoord", "mezelf dood", "mijzelf dood",
             "wil er niet meer zijn", "niet meer wakker worden", "wil niet meer bestaan"]),

        new("selfharm.hurt", ConcernCategory.SelfHarm, Severity.High,
            ["mezelf pijn doen", "mijzelf pijn doen", "mezelf pijn gedaan", "mijzelf pijn gedaan", "mezelf snijden",
             "mezelf gesneden", "mezelf krassen", "mezelf slaan", "mijn hoofd tegen de muur", "mezelf bijten"]),

        new("abuse.hitting", ConcernCategory.Abuse, Severity.High,
            ["slaat mij", "slaat me", "sloeg mij", "sloeg me", "schopt mij", "schopt me", "doet mij pijn", "doet me pijn",
             "deed mij pijn", "deed me pijn", "word geslagen", "werd geslagen", "knijpt mij", "knijpt me"]),
        new("abuse.touching", ConcernCategory.Abuse, Severity.High,
            ["aan mijn piemel", "aan mijn plassertje", "aan mijn plasser", "aan mijn billen", "aan mijn lijf",
             "aan mijn borsten", "geheimpje met een grote", "mag het niet vertellen van"]),

        new("neglect.alone", ConcernCategory.Neglect, Severity.High,
            ["niemand is thuis", "helemaal alleen thuis", "al heel lang alleen", "niemand zorgt voor mij",
             "geen eten gehad", "al dagen geen eten", "krijg geen eten"]),

        new("bullying", ConcernCategory.Bullying, Severity.Medium,
            ["pesten mij", "pesten me", "word gepest", "werd gepest", "ze pesten", "lachen mij uit", "lachen me uit"]),

        new("sadness.unloved", ConcernCategory.SevereSadness, Severity.Medium,
            ["niemand houdt van mij", "niemand vindt mij lief", "iedereen haat mij", "ik ben stom en dom"]),
        new("sadness.sad", ConcernCategory.SevereSadness, Severity.Low,
            ["heel verdrietig", "heel erg verdrietig", "moet steeds huilen", "ben bang"]),
    ];

    public Task<SafetyAssessment> AssessAsync(string text, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(text);

        var normalized = Normalize(text);
        SafetyAssessment result = SafetyAssessment.Safe(AssessmentSource.KeywordRule);

        foreach (var rule in Rules)
        {
            if (rule.Severity > result.Severity && rule.Phrases.Any(phrase => ContainsPhrase(normalized, phrase)))
            {
                result = new SafetyAssessment(rule.Severity, rule.Category, AssessmentSource.KeywordRule, rule.Id);
            }
        }

        return Task.FromResult(result);
    }

    /// <summary>Lower case, no accents, punctuation as spaces, single spaces, padded with a space.</summary>
    internal static string Normalize(string text)
    {
        var decomposed = text.ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length + 2).Append(' ');
        var lastWasSpace = true;

        foreach (var character in decomposed)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(character);
            if (category == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsLetterOrDigit(character))
            {
                builder.Append(character);
                lastWasSpace = false;
            }
            else if (!lastWasSpace)
            {
                builder.Append(' ');
                lastWasSpace = true;
            }
        }

        if (!lastWasSpace)
        {
            builder.Append(' ');
        }

        return builder.ToString();
    }

    private static bool ContainsPhrase(string normalized, string phrase) =>
        normalized.Contains($" {phrase} ", StringComparison.Ordinal);

    private sealed record SafetyRule(string Id, ConcernCategory Category, Severity Severity, string[] Phrases);
}
