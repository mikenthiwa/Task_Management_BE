using Application.Common.Interfaces;
using Application.Features.Tasks.Queries.GetTasksWithPagination;
using Ardalis.GuardClauses;
using AutoMapper.QueryableExtensions;
using Domain.Enum;
using Domain.Events;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Application.Features.Tasks.Command.UpdateTaskStatus;

public record UpdateTaskStatusCommand : IRequest
{
    [FromRoute] public Guid TaskId { get; set; }
    public required Status Status { get; init; }
}

public class UpdateTaskStatusCommandHandler(
    IApplicationDbContext applicationDb,
    IMapper mapper,
    ICurrentUserService currentUserService,
    INotificationPublisherService notificationPublisherService) : IRequestHandler<UpdateTaskStatusCommand>
{
    public async Task Handle(UpdateTaskStatusCommand request, CancellationToken cancellationToken)
    {
        var entity = await applicationDb.Tasks.FindAsync(new object[] { request.TaskId }, cancellationToken);
        Guard.Against.NotFound(request.TaskId, entity);

        // Ensure only the assigned user can update the task status
        if (entity.AssigneeId != currentUserService.UserId)
        {
            throw new UnauthorizedAccessException("Only the assigned user can update the task status");
        }

        var oldStatus = entity.Status;
        entity.Status = request.Status;
        entity.AddDomainEvent(new TaskStatusUpdatedEvent(entity.Id, entity.Title, oldStatus, request.Status, currentUserService.UserId!));

        await applicationDb.SaveChangesAsync(cancellationToken);

        var taskDto = await applicationDb.Tasks
            .AsNoTracking()
            .Where(task => task.Id == request.TaskId)
            .ProjectTo<TaskDto>(mapper.ConfigurationProvider)
            .FirstAsync(cancellationToken);

        await notificationPublisherService.NotifyTaskUpdatedAsync(taskDto);
    }
}
