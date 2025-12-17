using Domain.Common;
using Task = Domain.Entities.Task;

namespace Domain.Events;

public class TaskAssignedEvent(int taskId, string title, string assigneeId) : BaseEvent
{
    public int TaskId { get; } = taskId;
    public string Title { get; } = title;
    public string AssigneeId { get; } = assigneeId;
}
