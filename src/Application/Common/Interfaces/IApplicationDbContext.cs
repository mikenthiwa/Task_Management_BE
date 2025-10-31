using Domain.Entities;
using Task = Domain.Entities.Task;

namespace Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Task> Tasks {get;}
    DbSet<DomainUser> DomainUsers { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
    
}
