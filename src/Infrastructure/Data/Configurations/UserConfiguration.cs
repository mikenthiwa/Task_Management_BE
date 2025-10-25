using Domain.Entities;
using Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<DomainUser>
{
    public void Configure(EntityTypeBuilder<DomainUser> builder)
    {
        builder.ToTable("DomainUsers");
        builder.HasKey(u => u.Id);

        builder.HasOne<ApplicationUser>()
            .WithOne()
            .HasForeignKey<DomainUser>(u => u.Id)
            .HasPrincipalKey<ApplicationUser>(au => au.Id)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
