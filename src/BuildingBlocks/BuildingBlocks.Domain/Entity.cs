namespace BuildingBlocks.Domain;

/// <summary>An object with an identity that stays the same over time.</summary>
public abstract class Entity<TId>
    where TId : notnull
{
    protected Entity(TId id) => Id = id;

    public TId Id { get; }
}
