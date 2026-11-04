using Conversations.Domain;

namespace Conversations.Application.Ports;

/// <summary>
/// The participant of the current request. Until the Household and Identity modules exist,
/// a development implementation reads a fixed participant from configuration.
/// </summary>
public interface IParticipantContext
{
    ParticipantProfile Current { get; }
}

/// <param name="DisplayName">The name the gnome uses (a nickname, never a full name).</param>
/// <param name="TrustedAdults">How the participant calls trusted adults ("mama", "juf Anja"); the first is the primary one.</param>
/// <param name="AgeGroup">For example "kind" or "volwassene"; adults are spoken to simply but not childishly.</param>
/// <param name="DevelopmentNotes">What a caregiver told about the participant.</param>
public sealed record ParticipantProfile(
    ParticipantId Id,
    string DisplayName,
    IReadOnlyList<string> TrustedAdults,
    string? AgeGroup = null,
    string? DevelopmentNotes = null);
