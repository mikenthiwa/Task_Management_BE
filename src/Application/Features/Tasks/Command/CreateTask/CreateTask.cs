using Application.Common.Interfaces;
using Domain.Enum;
using Domain.Events;
using MediatR;
using Task = Domain.Entities.Task;

namespace Application.Features.Tasks.Command.CreateTask;

public record CreateTaskCommand : IRequest<int>
{
    public required string Title { get; init; }
    public string? Description { get; init; }
}
public class CreateTask(IApplicationDbContext applicationDbContext, ICurrentUserService currentUserService) : IRequestHandler<CreateTaskCommand, int>
{
    public async Task<int> Handle(CreateTaskCommand request, CancellationToken cancellationToken)
    {
        var entity = new Task
        {
            Title = request.Title,
            Description = request.Description,
            Status = 0,
            Priority = 0,
            CreatorId = currentUserService.UserId
        };
        entity.AddDomainEvent(new TaskCreatedEvent(entity));
        applicationDbContext.Tasks.Add(entity);

        await applicationDbContext.SaveChangesAsync(cancellationToken);
        return entity.Id;
    }
}
