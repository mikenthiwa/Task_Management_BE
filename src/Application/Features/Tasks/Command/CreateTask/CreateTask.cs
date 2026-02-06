using Application.Common.Interfaces;
using Application.Common.Options;
using Application.Features.Tasks.Caching;
using Domain.Enum;
using Domain.Events;
using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Task = Domain.Entities.Task;

namespace Application.Features.Tasks.Command.CreateTask;

public record CreateTaskCommand : IRequest<Guid>
{
    public required string Title { get; init; }
    public string? Description { get; init; }
}
public class CreateTask(
    IApplicationDbContext applicationDbContext,
    ICurrentUserService currentUserService,
    IMemoryCache cache,
    IOptions<TaskCachingOptions> cacheOptions) : IRequestHandler<CreateTaskCommand, Guid>
{
    public async Task<Guid> Handle(CreateTaskCommand request, CancellationToken cancellationToken)
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
        if (cacheOptions.Value.Enabled)
        {
            TaskCacheKey.BumpVersion(cache);
        }
        return entity.Id;
    }
}
