using Application.Common.Interfaces;
using Application.Common.Mappings;
using Application.Common.Models;
using Application.Features.Tasks.Queries.GetTasksWithPagination;
using AutoMapper.QueryableExtensions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Tasks.Queries.SearchTasks;

public record SearchTasksQuery : IRequest<PaginatedList<TaskDto>>
{
    public required string Query { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 10;
}

public class SearchTasksQueryHandler(
    IApplicationDbContext context,
    IMapper mapper) : IRequestHandler<SearchTasksQuery, PaginatedList<TaskDto>>
{
    public Task<PaginatedList<TaskDto>> Handle(SearchTasksQuery request, CancellationToken cancellationToken)
    {
        var query = request.Query.Trim();

        if (string.IsNullOrWhiteSpace(query))
        {
            return Task.FromResult(new PaginatedList<TaskDto>([], 0, request.PageNumber, request.PageSize));
        }

        return context.Tasks
            .AsNoTracking()
            .Where(task => task.SearchVector.Matches(
                EF.Functions.WebSearchToTsQuery("english", query)))
            .OrderByDescending(task => task.CreatedAt)
            .ProjectTo<TaskDto>(mapper.ConfigurationProvider)
            .PaginatedListAsync(request.PageNumber, request.PageSize);
    }
}
