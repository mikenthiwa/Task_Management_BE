using Application.Common.Interfaces;
using Application.Features.Tasks.Command.Queries.GetTasksWithPagination;
using Ardalis.GuardClauses;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Application.Features.Tasks.Command.AssignTask;

public record AssignTaskCommand : IRequest<TaskDto>
{
    public int TaskId { get; init; }
    public required string AssignedId { get; init; }
}
public class AssignTaskCommandHandler(IApplicationDbContext applicationDb, IMapper mapper) : IRequestHandler<AssignTaskCommand, TaskDto>
{
    public async Task<TaskDto> Handle(AssignTaskCommand request, CancellationToken cancellationToken)
    {
        var entity = await applicationDb.Tasks.FindAsync(new object[] { request.TaskId }, cancellationToken);
        Guard.Against.NotFound(request.TaskId, entity);
        
        entity.AssigneeId = request.AssignedId;
        await applicationDb.SaveChangesAsync(cancellationToken);
        return mapper.Map<TaskDto>(entity);
    }
    
}
