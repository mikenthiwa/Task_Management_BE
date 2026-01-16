using Application.Common.Interfaces;
using Application.Common.Mappings;
using Application.Common.Models;
using Application.Features.Tasks.Command.Queries.GetTasksWithPagination;
using AutoMapper.QueryableExtensions;
using Domain.Enum;
using MediatR;

namespace Application.Features.Tasks.Queries.GetTasksWithPagination;

public record GetTaskWithQuery : IRequest<PaginatedList<TaskDto>>
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public Status? Status { get; init; }
    public string? AssigneeId { get; init; }
}

public class GetTasksWithPaginationHandler(IApplicationDbContext context, IMapper mapper) : IRequestHandler<GetTaskWithQuery, PaginatedList<TaskDto>>
{
    public async Task<PaginatedList<TaskDto>> Handle(GetTaskWithQuery request, CancellationToken cancellationToken)
    {
        return await context.Tasks
            .AsNoTracking()
            .TaskQuery(request)
            .OrderByDescending(t => t.Id)
            .ProjectTo<TaskDto>(mapper.ConfigurationProvider)
            .PaginatedListAsync(request.PageNumber, request.PageSize);
    }
}
