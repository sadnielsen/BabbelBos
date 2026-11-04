using Conversations.Domain;

namespace Conversations.Application.Ports;

/// <summary>
/// Port: checks one text (from a participant or from the LLM) for concerns.
/// Implementations may throw; callers fail closed (see <see cref="Safety.SafetyClassifierExtensions"/>).
/// </summary>
public interface ISafetyClassifier
{
    Task<SafetyAssessment> AssessAsync(string text, CancellationToken cancellationToken);
}
