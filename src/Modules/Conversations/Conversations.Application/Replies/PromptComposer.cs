using System.Text.Json;

using Conversations.Application.Ports;
using Conversations.Domain;

using GnomeCatalog.Contracts;

namespace Conversations.Application.Replies;

/// <summary>
/// Builds the LLM request: the gnome's system prompt with the participant's context filled in,
/// plus the last messages of this conversation.
/// </summary>
public static class PromptComposer
{
    /// <summary>How many earlier messages go into the prompt verbatim.</summary>
    public const int HistoryLength = 10;

    // Own day names: the API runs with invariant globalization, so cultures are not available.
    private static readonly string[] DutchDays = ["zondag", "maandag", "dinsdag", "woensdag", "donderdag", "vrijdag", "zaterdag"];

    public static ChatRequest Compose(
        GnomeProfile gnome,
        ParticipantProfile participant,
        Conversation conversation,
        DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(gnome);
        ArgumentNullException.ThrowIfNull(conversation);

        var values = TemplateRenderer.ParticipantValues(participant, gnome.Name);
        values["local_time_description"] = DescribeTime(now);

        // Memory (summary and facts) arrives in a later iteration; until then the template says "nothing known".
        var systemPrompt = TemplateRenderer.Render(gnome.SystemPromptTemplate, values);

        var turns = conversation.Messages
            .TakeLast(HistoryLength + 1)
            .Select(ToTurn)
            .ToList();

        return new ChatRequest(systemPrompt, turns);
    }

    private static ChatTurn ToTurn(Message message) =>
        message.Author == MessageAuthor.Participant
            ? new ChatTurn(ChatTurnRole.Participant, message.Text)
            : new ChatTurn(ChatTurnRole.Gnome, JsonSerializer.Serialize(new { reply = message.Text, concern = (object?)null }));

    internal static string DescribeTime(DateTimeOffset now)
    {
        var local = TimeZoneInfo.ConvertTime(now, AmsterdamTimeZone());
        var day = DutchDays[(int)local.DayOfWeek];
        var part = local.Hour switch
        {
            < 6 => "nacht",
            < 12 => "ochtend",
            < 18 => "middag",
            _ => "avond",
        };

        return day + part;
    }

    private static TimeZoneInfo AmsterdamTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Europe/Amsterdam");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.Utc;
        }
    }
}
