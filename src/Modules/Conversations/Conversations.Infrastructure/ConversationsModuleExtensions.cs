using Conversations.Application.Ports;
using Conversations.Application.Replies;
using Conversations.Application.Safety;
using Conversations.Infrastructure.Llm;
using Conversations.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Conversations.Infrastructure;

/// <summary>Registers the Conversations module in the host.</summary>
public static class ConversationsModuleExtensions
{
    public const string ConnectionStringName = "babbelbos";

    public static IServiceCollection AddConversationsModule(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(ConnectionStringName)
            ?? throw new InvalidOperationException($"Connection string '{ConnectionStringName}' is missing.");

        // Options as a plain instance (not AddDbContext's factory lambda), so Wolverine's generated
        // handler code can construct the DbContext directly instead of using service location.
        services.AddSingleton(new DbContextOptionsBuilder<ConversationsDbContext>()
            .UseConversationsNpgsql(connectionString)
            .Options);
        services.AddScoped<ConversationsDbContext>();
        services.AddHealthChecks().AddDbContextCheck<ConversationsDbContext>("conversations-db");

        services.AddScoped<IConversationRepository, ConversationRepository>();
        services.AddScoped<IConversationQueries, ConversationQueries>();

        // Iteration 2: keyword rules only. Iteration 3 moves safety into the Safeguarding module.
        services.AddSingleton<ISafetyClassifier, KeywordSafetyClassifier>();
        services.AddScoped<GnomeReplyService>();
        services.AddLlm(configuration);

        return services;
    }

    /// <summary>Applies pending migrations. Development and tests only; production uses a migration job.</summary>
    public static async Task MigrateConversationsDatabaseAsync(this IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ConversationsDbContext>();
        await db.Database.MigrateAsync().ConfigureAwait(false);
    }
}
