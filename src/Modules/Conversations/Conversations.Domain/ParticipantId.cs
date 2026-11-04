namespace Conversations.Domain;

/// <summary>
/// Reference to a participant. The Household module owns participants; this module only keeps the id.
/// </summary>
public readonly record struct ParticipantId(Guid Value)
{
    public override string ToString() => Value.ToString();
}
