using Domain.Entities;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Task = Domain.Entities.Task;

namespace Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Task> Tasks {get;}
    DbSet<DomainUser> DomainUsers { get; }
    DbSet<Notification> Notifications { get; }
    DbSet<ReportJob> ReportJobs { get; }
    EntityEntry<TEntity> Entry<TEntity>(TEntity entity) where TEntity : class;
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
    
}
