using Task = Domain.Entities.Task;

namespace Application.Features.Tasks.Queries.GetTasksWithPagination;

public static class TaskFilterExtensions
{
    public static IQueryable<Task> ApplyTaskFilters(this IQueryable<Task> queryable, GetTaskWithQuery request)
    {
        var query = queryable;
        var assigneeId = request.AssigneeId?.Trim();
        var searchTerm = request.SearchTerm?.Trim();

        if (request.Status.HasValue)
        {
            query = query.Where(task => task.Status == request.Status.Value);
        }

        if (!string.IsNullOrEmpty(assigneeId))
        {
            query = query.Where(task => task.AssigneeId == assigneeId);
        }

        if (!string.IsNullOrEmpty(searchTerm))
        {
            var searchQuery = EF.Functions.WebSearchToTsQuery("english", searchTerm);
            query = query.Where(task => task.SearchVector.Matches(searchQuery));
        }

        return query;
    }
}
