using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Babbelbos.Api.Tests;

/// <summary>
/// The real API with a real PostgreSQL database and the fake LLM. Tenant and participant
/// come from configuration until sign-in exists (iteration 4).
/// </summary>
public sealed class BabbelbosApiFactory(string connectionString, Guid tenantId, Guid participantId) : WebApplicationFactory<Program>
{
    public static async Task<BabbelbosApiFactory> CreateAsync(PostgresFixture postgres, Guid? tenantId = null, Guid? participantId = null)
    {
        var connectionString = await postgres.CreateDatabaseAsync();
        return new BabbelbosApiFactory(connectionString, tenantId ?? Guid.NewGuid(), participantId ?? Guid.NewGuid());
    }

    /// <summary>The same database, seen by another tenant and participant.</summary>
    public BabbelbosApiFactory AsOtherTenant() => new(connectionString, Guid.NewGuid(), Guid.NewGuid());

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:babbelbos", connectionString);
        builder.UseSetting("Llm:Provider", "Fake");
        builder.UseSetting("Development:TenantId", tenantId.ToString());
        builder.UseSetting("Development:Participant:Id", participantId.ToString());
        builder.UseSetting("Development:Participant:DisplayName", "Sam");
        builder.UseSetting("Development:Participant:TrustedAdults:0", "mama");
        builder.UseSetting("Development:Participant:TrustedAdults:1", "juf Anja");
    }
}
