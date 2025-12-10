using Application.Common.Interfaces;
using Application.Features.Tasks.Command.Queries.GetTasksWithPagination;
using Ardalis.GuardClauses;
using Domain.Enum;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Application.Features.Tasks.Command.AssignTask;

public record AssignTaskCommand : IRequest<TaskDto>
{
    [FromRoute] public int TaskId { get; set; }
    public required string AssignedId { get; init; }
}
public class AssignTaskCommandHandler(IApplicationDbContext applicationDb, IMapper mapper, INotificationService notificationService) : IRequestHandler<AssignTaskCommand, TaskDto>
{
    public async Task<TaskDto> Handle(AssignTaskCommand request, CancellationToken cancellationToken)
    {
        var entity = await applicationDb.Tasks.FindAsync(new object[] { request.TaskId }, cancellationToken);
        Guard.Against.NotFound(request.TaskId, entity);
        
        entity.AssigneeId = request.AssignedId;
        await applicationDb.SaveChangesAsync(cancellationToken);
        await notificationService.CreateNotificationAsync(request.AssignedId, $"You have been assigned to task '{entity.Title}'.", NotificationType.TaskAssigned);
        return mapper.Map<TaskDto>(entity);
    }
    
}
