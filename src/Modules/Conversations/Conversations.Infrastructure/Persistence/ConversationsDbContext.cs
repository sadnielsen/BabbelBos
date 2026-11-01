using BuildingBlocks.Application;
using BuildingBlocks.Domain;

using Conversations.Contracts;
using Conversations.Domain;

using Microsoft.EntityFrameworkCore;

namespace Conversations.Infrastructure.Persistence;

/// <summary>
/// EF Core mapping of the Conversations module, in its own schema. Every query is filtered on
/// the current tenant. Row-level security in PostgreSQL follows in iteration 7 (see design §13).
/// </summary>
public sealed class ConversationsDbContext(DbContextOptions<ConversationsDbContext> options, ITenantContext tenant)
    : DbContext(options)
{
    public DbSet<Conversation> Conversations => Set<Conversation>();

    // A member of the context, so EF parameterises the filter per context instance.
    private TenantId CurrentTenantId => tenant.TenantId;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.HasDefaultSchema(ConversationModule.Schema);

        modelBuilder.Entity<Conversation>(conversation =>
        {
            conversation.ToTable("conversations");
            conversation.HasKey(c => c.Id);
            conversation.Property(c => c.Id).HasConversion(id => id.Value, value => new ConversationId(value)).ValueGeneratedNever();
            conversation.Property(c => c.TenantId).HasConversion(id => id.Value, value => new TenantId(value));
            conversation.Property(c => c.ParticipantId).HasConversion(id => id.Value, value => new ParticipantId(value));
            conversation.Property(c => c.GnomeId).HasConversion(id => id.Value, value => new GnomeId(value)).HasMaxLength(50);
            conversation.Property(c => c.StartedAt);
            conversation.Property(c => c.ClosedAt);
            conversation.Ignore(c => c.IsClosed);
            conversation.Ignore(c => c.DomainEvents);

            conversation.HasIndex(c => new { c.TenantId, c.ParticipantId, c.StartedAt });
            conversation.HasQueryFilter(c => c.TenantId == CurrentTenantId);

            conversation.HasMany(c => c.Messages)
                .WithOne()
                .HasForeignKey("ConversationId")
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);
            conversation.Navigation(c => c.Messages)
                .HasField("_messages")
                .UsePropertyAccessMode(PropertyAccessMode.Field)
                .AutoInclude();
        });

        modelBuilder.Entity<Message>(message =>
        {
            message.ToTable("messages");
            message.HasKey(m => m.Id);
            message.Property(m => m.Id).HasConversion(id => id.Value, value => new MessageId(value)).ValueGeneratedNever();
            message.Property(m => m.Author).HasConversion<string>().HasMaxLength(20);
            message.Property(m => m.Text).HasMaxLength(2000);
            message.Property(m => m.SentAt);
            message.Property(m => m.ReplyKind).HasConversion<string>().HasMaxLength(20);
            message.Property(m => m.PromptVersion).HasMaxLength(100);

            message.ComplexProperty(m => m.Safety, safety =>
            {
                safety.Property(s => s.Severity).HasConversion<string>().HasMaxLength(20).HasColumnName("safety_severity");
                safety.Property(s => s.Category).HasConversion<string>().HasMaxLength(30).HasColumnName("safety_category");
                safety.Property(s => s.Source).HasConversion<string>().HasMaxLength(20).HasColumnName("safety_source");
                safety.Property(s => s.RuleId).HasMaxLength(50).HasColumnName("safety_rule_id");
            });

            message.HasIndex("ConversationId", nameof(Message.SentAt));
        });
    }
}
