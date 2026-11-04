using Npgsql;

using Testcontainers.PostgreSql;

[assembly: AssemblyFixture(typeof(Babbelbos.Api.Tests.PostgresFixture))]

namespace Babbelbos.Api.Tests;

/// <summary>
/// One PostgreSQL for all integration tests. Uses Testcontainers (Docker), or an existing server when
/// <c>BABBELBOS_TEST_POSTGRES</c> holds a connection string. Each factory gets its own fresh database.
/// </summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    public const string ExistingServerVariable = "BABBELBOS_TEST_POSTGRES";

    private PostgreSqlContainer? _container;
    private string _serverConnectionString = string.Empty;

    public async ValueTask InitializeAsync()
    {
        var existing = Environment.GetEnvironmentVariable(ExistingServerVariable);
        if (!string.IsNullOrWhiteSpace(existing))
        {
            _serverConnectionString = existing;
            return;
        }

        _container = new PostgreSqlBuilder("postgres:17-alpine").Build();
        await _container.StartAsync();
        _serverConnectionString = _container.GetConnectionString();
    }

    /// <summary>Creates an empty database and returns its connection string.</summary>
    public async Task<string> CreateDatabaseAsync()
    {
        var name = $"babbelbos_test_{Guid.NewGuid():N}";

        await using var connection = new NpgsqlConnection(_serverConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand($"CREATE DATABASE \"{name}\"", connection);
        await command.ExecuteNonQueryAsync();

        return new NpgsqlConnectionStringBuilder(_serverConnectionString) { Database = name }.ConnectionString;
    }

    public async ValueTask DisposeAsync()
    {
        if (_container is not null)
        {
            await _container.DisposeAsync();
        }
    }
}
