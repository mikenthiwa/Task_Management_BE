using Task = Domain.Entities.Task;

namespace Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Task> Tasks {get;}
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
