using Conversations.Contracts;

using Microsoft.EntityFrameworkCore;

namespace Conversations.Infrastructure.Persistence;

internal static class NpgsqlOptions
{
    public static TBuilder UseConversationsNpgsql<TBuilder>(
        this TBuilder builder,
        string connectionString)
        where TBuilder : DbContextOptionsBuilder
    {
        builder.UseNpgsql(connectionString, npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", ConversationModule.Schema))
            .UseSnakeCaseNamingConvention();
        return builder;
    }
}
