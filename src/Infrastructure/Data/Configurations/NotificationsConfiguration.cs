using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

public class NotificationsConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("Notifications");
        builder.HasKey(notification => notification.Id);
        
        builder.Property(notification => notification.UserId )
            .IsRequired();

        builder.Property(notification => notification.Message)
            .IsRequired();

        builder.Property(notification => notification.Type)
            .IsRequired();

        builder.HasOne(notification => notification.User)
            .WithMany(user => user.Notifications)
            .HasForeignKey(notification => notification.UserId)
            .HasPrincipalKey(domainUser => domainUser.Id)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(notification => notification.UserId);
        builder.HasIndex(notification => notification.IsRead);
    }
}
