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
            .HasMaxLength(450)
            .IsRequired();

        builder.Property(notification => notification.Message)
            .HasMaxLength(1024)
            .IsRequired();

        builder.OwnsOne(notification => notification.Action, actionBuilder =>
        {
            actionBuilder.Property(a => a.ActionUrl)
                .HasMaxLength(2048)
                .IsRequired();
            actionBuilder.Property(a => a.ActionLabel)
                .HasMaxLength(256)
                .IsRequired();
        });

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
