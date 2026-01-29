using Domain.Common;
using Task = Domain.Entities.Task;

namespace Domain.Events;

public class TaskAssignedEvent(Guid taskId, string title, string assigneeId) : BaseEvent
{
    public Guid TaskId { get; } = taskId;
    public string Title { get; } = title;
    public string AssigneeId { get; } = assigneeId;
}
