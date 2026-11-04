using System.Text.Json;
using System.Text.Json.Serialization;

using Conversations.Domain;

namespace Conversations.Application.Replies;

/// <summary>Reads the JSON answer of the LLM: <c>{"reply": "...", "concern": {...} | null}</c>.</summary>
public static class GnomeReplyParser
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
    };

    public static ParsedReply? TryParse(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        // Models sometimes wrap JSON in a code fence or add text around it.
        var start = raw.IndexOf('{', StringComparison.Ordinal);
        var end = raw.LastIndexOf('}');
        if (start < 0 || end <= start)
        {
            return null;
        }

        try
        {
            var dto = JsonSerializer.Deserialize<ReplyDto>(raw[start..(end + 1)], Options);
            if (dto is null || string.IsNullOrWhiteSpace(dto.Reply))
            {
                return null;
            }

            var selfReport = dto.Concern is { } concern
                ? new SafetyAssessment(concern.Severity, concern.Category, AssessmentSource.LlmSelfReport)
                : SafetyAssessment.Safe(AssessmentSource.LlmSelfReport);

            return new ParsedReply(dto.Reply.Trim(), selfReport);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private sealed record ReplyDto(string? Reply, ConcernDto? Concern);

    private sealed record ConcernDto(ConcernCategory? Category, Severity Severity);
}

/// <param name="Reply">What the gnome says.</param>
/// <param name="SelfReport">The model's own concern report; one of several signals, never the only one.</param>
public sealed record ParsedReply(string Reply, SafetyAssessment SelfReport);
