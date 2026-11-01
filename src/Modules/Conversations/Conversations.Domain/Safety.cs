namespace Conversations.Domain;

/// <summary>How serious a concern is. High and Critical always get a fixed safety script.</summary>
public enum Severity
{
    None = 0,
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4,
}

/// <summary>Kind of concern. The names match the keys of the gnome safety scripts.</summary>
public enum ConcernCategory
{
    SelfHarm,
    SuicidalIdeation,
    Abuse,
    Neglect,
    MedicalEmergency,
    Bullying,
    SevereSadness,
    Other,
}

/// <summary>Which check produced an assessment.</summary>
public enum AssessmentSource
{
    KeywordRule,
    Classifier,
    LlmSelfReport,

    /// <summary>The safety check itself failed; the system failed closed.</summary>
    CheckFailed,
}
