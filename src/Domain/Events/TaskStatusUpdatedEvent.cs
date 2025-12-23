using Domain.Common;
using Domain.Enum;

namespace Domain.Events;

public class TaskStatusUpdatedEvent(int taskId, string title, Status oldStatus, Status newStatus, string updatedBy) : BaseEvent
{
    public int TaskId { get; } = taskId;
    public string Title { get; } = title;
    public Status OldStatus { get; } = oldStatus;
    public Status NewStatus { get; } = newStatus;
    public string UpdatedBy { get; } = updatedBy;
}
