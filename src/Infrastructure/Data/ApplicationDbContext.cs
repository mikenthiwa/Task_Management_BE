using System.Reflection;
using Application.Common.Interfaces;
using Domain.Entities;
using Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Task = Domain.Entities.Task;
using Notification = Domain.Entities.Notification;

namespace Infrastructure.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<ApplicationUser>(options), IApplicationDbContext
{
    public DbSet<Task> Tasks => Set<Task>();
    public DbSet<DomainUser> DomainUsers => Set<DomainUser>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<ReportJob> ReportJobs => Set<ReportJob>();
    
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}
