namespace BuildingBlocks.Domain;

/// <summary>Identity of a tenant (a family or a care organisation). Shared by every module.</summary>
public readonly record struct TenantId(Guid Value)
{
    public override string ToString() => Value.ToString();
}
