namespace Conversations.Application.Replies;

/// <summary>
/// Cheap structural checks on generated text before it reaches a participant.
/// The safety classifier runs on the text as well; this guard only checks the form.
/// </summary>
public static class ReplyGuard
{
    /// <summary>The persona asks for at most 40 words; this is the hard limit with some slack.</summary>
    public const int MaxWords = 60;

    private static readonly string[] ForbiddenFragments = ["http://", "https://", "www.", "```", "**"];

    public static bool IsAcceptable(string reply)
    {
        if (string.IsNullOrWhiteSpace(reply))
        {
            return false;
        }

        var words = reply.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;
        if (words > MaxWords)
        {
            return false;
        }

        return !ForbiddenFragments.Any(fragment => reply.Contains(fragment, StringComparison.OrdinalIgnoreCase));
    }
}
