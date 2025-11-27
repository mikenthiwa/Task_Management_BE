using Domain.Entities;
using Task = Domain.Entities.Task;

namespace Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Task> Tasks {get;}
    DbSet<DomainUser> DomainUsers { get; }
    DbSet<Notification> Notifications { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
    
}
