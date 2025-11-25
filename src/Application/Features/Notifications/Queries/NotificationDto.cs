using Domain.Entities;
using Domain.Enum;

namespace Application.Features.Notifications.Queries;

public class NotificationDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public bool IsRead { get; set; }
    public NotificationType NotificationType { get; set; }
    
    public class Mapping: Profile
    {
        public Mapping()
        {
            CreateMap<Notification, NotificationDto>();
        }
    }
    
}
