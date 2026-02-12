using Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NpgsqlTypes;
using Task = Domain.Entities.Task;

namespace Infrastructure.Data.Configurations;

public class TaskConfiguration : IEntityTypeConfiguration<Task>
{
    public void Configure(EntityTypeBuilder<Task> builder)
    {
        builder.ToTable("Tasks");

        builder.HasOne(task => task.Assignee)
            .WithMany(u => u.AssignedTasks)
            .HasForeignKey(t => t.AssigneeId)
            .HasPrincipalKey(u => u.Id)
            .OnDelete(DeleteBehavior.SetNull);
        
        builder.HasOne(task => task.Creator)
            .WithMany(u => u.CreatedTasks)
            .HasForeignKey(t => t.CreatorId)
            .HasPrincipalKey(u => u.Id)
            .OnDelete(DeleteBehavior.SetNull);
        
        
        builder.HasIndex(t => t.AssigneeId);
        builder.HasIndex(t => t.CreatorId);
        builder.HasIndex(t => t.CreatedAt);
        builder.HasIndex(t => new { t.Status, t.Priority });

        builder.Property(t => t.SearchVector)
            .HasColumnType("tsvector")
            .HasComputedColumnSql(
                "to_tsvector('english', coalesce(\"Title\", '') || ' ' || coalesce(\"Description\", ''))",
                stored: true);
        builder.HasIndex(t => t.SearchVector).HasMethod("GIN");
    }
}
