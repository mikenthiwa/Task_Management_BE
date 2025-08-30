using Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Task = Domain.Entities.Task;

namespace Infrastructure.Data.Configurations;

public class TaskConfiguration : IEntityTypeConfiguration<Task>
{
    public void Configure(EntityTypeBuilder<Task> entity)
    {
        entity.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(t => t.AssigneeId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(t => t.CreatorId)
            .OnDelete(DeleteBehavior.Restrict);
        
        entity.HasIndex(t => t.AssigneeId);
        entity.HasIndex(t => t.CreatorId);
        entity.HasIndex(t => new { t.Status, t.Priority });
    }
}
