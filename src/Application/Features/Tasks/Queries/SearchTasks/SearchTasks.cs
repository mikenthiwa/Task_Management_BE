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
        var tsQuery = EF.Functions.WebSearchToTsQuery("english", request.Query.Trim());

        return context.Tasks
            .AsNoTracking()
            .Where(task => task.SearchVector != null && task.SearchVector.Matches(tsQuery))
            .OrderByDescending(task => EF.Functions.TsRank(task.SearchVector!, tsQuery))
            .ThenByDescending(task => task.CreatedAt)
            .ProjectTo<TaskDto>(mapper.ConfigurationProvider)
            .PaginatedListAsync(request.PageNumber, request.PageSize);
    }
}
