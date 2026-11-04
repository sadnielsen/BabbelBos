namespace Conversations.Domain;

/// <summary>
/// The result of a safety check on one message. Holds no message text, only the outcome,
/// so it can be logged and sent to other modules.
/// </summary>
public sealed record SafetyAssessment(
    Severity Severity,
    ConcernCategory? Category,
    AssessmentSource Source,
    string? RuleId = null)
{
    /// <summary>Nothing found by the given check.</summary>
    public static SafetyAssessment Safe(AssessmentSource source) => new(Severity.None, null, source);

    /// <summary>The check could not run. The system must treat this as unsafe (fail closed).</summary>
    public static SafetyAssessment CheckFailed() => new(Severity.None, null, AssessmentSource.CheckFailed);

    public bool HasCheckFailed => Source == AssessmentSource.CheckFailed;

    /// <summary>True when the gnome may not answer with LLM text and must use a fixed script.</summary>
    public bool RequiresFixedReply => HasCheckFailed || Severity >= Severity.High;

    /// <summary>The more serious of two assessments. A failed check always wins.</summary>
    public SafetyAssessment MostSevere(SafetyAssessment other)
    {
        ArgumentNullException.ThrowIfNull(other);

        if (HasCheckFailed)
        {
            return this;
        }

        if (other.HasCheckFailed)
        {
            return other;
        }

        return other.Severity > Severity ? other : this;
    }
}
