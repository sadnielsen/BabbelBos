using Conversations.Application.Ports;
using Conversations.Domain;

using Microsoft.Extensions.Logging;

namespace Conversations.Application.Safety;

public static partial class SafetyClassifierExtensions
{
    /// <summary>
    /// Runs the classifier and fails closed: when the check itself fails, the result is
    /// <see cref="SafetyAssessment.CheckFailed"/>, which always leads to a fixed reply.
    /// </summary>
    public static async Task<SafetyAssessment> AssessOrFailClosedAsync(
        this ISafetyClassifier classifier,
        string text,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(classifier);

        try
        {
            return await classifier.AssessAsync(text, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            LogCheckFailed(logger, exception);
            return SafetyAssessment.CheckFailed();
        }
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Safety check failed; failing closed")]
    private static partial void LogCheckFailed(ILogger logger, Exception exception);
}
