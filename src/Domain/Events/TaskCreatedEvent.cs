using Domain.Common;
using Task = Domain.Entities.Task;

namespace Domain.Events;

public class TaskCreatedEvent(Task task) : BaseEvent
{
    public Task TaskItem { get; } = task;
}
