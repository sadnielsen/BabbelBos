using BuildingBlocks.Domain;

namespace BuildingBlocks.Application;

/// <summary>
/// The tenant of the current request. Resolved from the access token only,
/// never from the request body or query string.
/// </summary>
public interface ITenantContext
{
    TenantId TenantId { get; }
}
