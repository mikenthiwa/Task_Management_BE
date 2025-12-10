using Task = Domain.Entities.Task;

namespace Application.Features.Tasks.Queries.GetTasksWithPagination;

public static class TaskQueryExtension
{
    public static IQueryable<Task> TaskQuery(this IQueryable<Task> queryable, GetTaskWithQuery request)
    {
        var query = queryable.AsQueryable();
        if (request.Status.HasValue)
        {
            query = query.Where(task => task.Status == request.Status.Value);
        }
        if (!string.IsNullOrEmpty(request.AssigneeId))
        {
            query = query.Where(t => t.AssigneeId == request.AssigneeId);
        }

        return query;

    }
}
