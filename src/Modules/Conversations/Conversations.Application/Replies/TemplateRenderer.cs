using System.Text.RegularExpressions;

using Conversations.Application.Ports;

namespace Conversations.Application.Replies;

/// <summary>Fills <c>{{placeholders}}</c> in prompts, greetings and safety scripts.</summary>
public static partial class TemplateRenderer
{
    public const string Unknown = "(nog niets bekend)";
    public const string AnyTrustedAdult = "een grote mens die je vertrouwt";

    public static string Render(string template, IReadOnlyDictionary<string, string> values)
    {
        ArgumentNullException.ThrowIfNull(template);
        ArgumentNullException.ThrowIfNull(values);

        return Placeholder().Replace(template, match =>
            values.TryGetValue(match.Groups[1].Value, out var value) && !string.IsNullOrWhiteSpace(value)
                ? value
                : Unknown);
    }

    /// <summary>The values that are always available for a participant and gnome.</summary>
    public static Dictionary<string, string> ParticipantValues(ParticipantProfile participant, string gnomeName)
    {
        ArgumentNullException.ThrowIfNull(participant);

        var trustedAdults = participant.TrustedAdults.Where(a => !string.IsNullOrWhiteSpace(a)).ToList();

        return new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["participant_name"] = participant.DisplayName,
            ["gnome_name"] = gnomeName,
            ["trusted_adults"] = trustedAdults.Count > 0 ? string.Join(", ", trustedAdults) : AnyTrustedAdult,
            ["primary_trusted_adult"] = trustedAdults.Count > 0 ? trustedAdults[0] : AnyTrustedAdult,
            ["participant_age_group"] = participant.AgeGroup ?? "kind",
            ["development_notes"] = participant.DevelopmentNotes ?? Unknown,
        };
    }

    [GeneratedRegex(@"\{\{\s*([a-z_]+)\s*\}\}", RegexOptions.CultureInvariant)]
    private static partial Regex Placeholder();
}
