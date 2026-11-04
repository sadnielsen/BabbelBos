namespace BuildingBlocks.Application;

/// <summary>Current time, injectable so handlers stay deterministic in tests.</summary>
public interface IClock
{
    DateTimeOffset UtcNow { get; }
}

/// <summary>The real clock.</summary>
public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
