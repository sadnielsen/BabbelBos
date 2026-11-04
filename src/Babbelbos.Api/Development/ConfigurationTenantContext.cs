using BuildingBlocks.Application;
using BuildingBlocks.Domain;

namespace Babbelbos.Api.Development;

/// <summary>
/// TEMPORARY until the Identity module (iteration 4): the tenant comes from configuration
/// (<c>Development:TenantId</c>). Without that setting every request fails, so this can never
/// silently run in production.
/// </summary>
public sealed class ConfigurationTenantContext(IConfiguration configuration) : ITenantContext
{
    public TenantId TenantId => Guid.TryParse(configuration["Development:TenantId"], out var id)
        ? new TenantId(id)
        : throw new InvalidOperationException("No tenant: Development:TenantId is not configured and there is no sign-in yet.");
}
