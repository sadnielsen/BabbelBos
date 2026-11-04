using BuildingBlocks.Application;
using BuildingBlocks.Domain;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Conversations.Infrastructure.Persistence;

/// <summary>Used by <c>dotnet ef migrations</c> only; never at runtime.</summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ConversationsDbContext>
{
    public ConversationsDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<ConversationsDbContext>()
            .UseConversationsNpgsql("Host=localhost;Database=babbelbos_design")
            .Options;

        return new ConversationsDbContext(options, new DesignTimeTenant());
    }

    private sealed class DesignTimeTenant : ITenantContext
    {
        public TenantId TenantId => new(Guid.Empty);
    }
}
