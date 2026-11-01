namespace BuildingBlocks.Domain;

/// <summary>An expected domain failure, identified by a stable code.</summary>
public sealed record DomainError(string Code, string Message)
{
    public static readonly DomainError None = new(string.Empty, string.Empty);
}
