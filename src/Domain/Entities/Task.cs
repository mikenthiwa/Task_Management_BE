using Domain.Common;
using Domain.Enum;

namespace Domain.Entities;

public class Task : BaseAuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string Title { get; set; }
    public string? Description { get; set; }
    public Status Status { get; set; }
    public Priority Priority { get; set; }

    public string? AssigneeId { get; set; }
    public string? CreatorId { get; set; }
    
    public DomainUser? Assignee { get; set; }
    public DomainUser? Creator { get; set; }
    
}
