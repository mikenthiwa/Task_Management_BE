using Infrastructure.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

public sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("OutboxMessages");
        builder.HasKey(message => message.Id);

        builder.Property(message => message.Type)
            .HasMaxLength(512)
            .IsRequired();
        builder.Property(message => message.Payload)
            .HasColumnType("jsonb")
            .IsRequired();
        builder.Property(message => message.Exchange)
            .HasMaxLength(256)
            .IsRequired();
        builder.Property(message => message.RoutingKey)
            .HasMaxLength(256)
            .IsRequired();
        builder.Property(message => message.LastError)
            .HasMaxLength(4096);
        builder.Property(message => message.LeaseId)
            .IsConcurrencyToken();

        builder.HasIndex(message => new { message.NextAttemptAt, message.LeasedUntil, message.OccurredAt })
            .HasDatabaseName("IX_OutboxMessages_Pending")
            .HasFilter("\"ProcessedAt\" IS NULL AND \"FailedAt\" IS NULL");
        builder.HasIndex(message => message.ProcessedAt);
    }
}
