using Application.Common.Interfaces;
// using Application.Features.Tasks.Command.Queries.GetTasksWithPagination;
using Application.Features.Tasks.Queries.GetTasksWithPagination;
using Ardalis.GuardClauses;
using AutoMapper.QueryableExtensions;
using Domain.Events;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Application.Features.Tasks.Command.AssignTask;

public record AssignTaskCommand : IRequest
{
    [FromRoute] public Guid TaskId { get; set; }
    public required string AssignedId { get; init; }
}

public class AssignTaskCommandHandler(IApplicationDbContext applicationDb, IMapper mapper, INotificationPublisherService notificationPublisherService) : IRequestHandler<AssignTaskCommand>
{
    public async Task Handle(AssignTaskCommand request, CancellationToken cancellationToken)
    {
        var entity = await applicationDb.Tasks.FindAsync( [request.TaskId], cancellationToken);
        Guard.Against.NotFound(request.TaskId, entity);
        entity.AssigneeId = request.AssignedId;
        entity.AddDomainEvent(new TaskAssignedEvent(entity.Id, entity.Title, request.AssignedId));
        await applicationDb.SaveChangesAsync(cancellationToken);
        
        var taskDto = await applicationDb.Tasks
            .AsNoTracking()
            .Where(task => task.Id == request.TaskId)
            .ProjectTo<TaskDto>(mapper.ConfigurationProvider)
            .FirstAsync(cancellationToken);
        await notificationPublisherService.NotifyTaskUpdatedAsync(taskDto);
    }
    
}
