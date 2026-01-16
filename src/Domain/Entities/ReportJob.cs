using Domain.Common;

namespace Domain.Entities;

public class ReportJob : BaseAuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string ReportType { get; set; } = "Tasks";
    
    public DateTimeOffset From { get; set; }
    public DateTimeOffset To { get; set; }
    
    public required string RequestedByUserId { get; set; }
    
    public string Status { get; set; } = "Pending";
    
    public string? FilePath { get; set; }
    public string? ErrorMessage { get; set; }
    
    public DateTimeOffset? CompletedAt { get; set; }
    
}
