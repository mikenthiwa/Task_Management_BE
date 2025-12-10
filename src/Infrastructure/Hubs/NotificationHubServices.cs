using Application.Common.Interfaces;
using Application.Features.Notifications.Queries;
using AutoMapper;
using Domain.Entities;
using Microsoft.AspNetCore.SignalR;
using Task = System.Threading.Tasks.Task;

namespace Infrastructure.Hubs;

public class NotificationHubServices(IHubContext<NotificationHub> hubContext, IMapper mapper) : INotificationPublisherService
{
    public async Task PublishAsync(Notification notification)
    {
        var notificationDto = mapper.Map<NotificationDto>(notification);
        await hubContext.Clients.All.SendAsync("ReceiveNotification", notificationDto);
    }
    
    public async Task PublishToUserAsync(string userId, Notification notification)
    {
        var notificationDto = mapper.Map<NotificationDto>(notification);
        await hubContext.Clients.Group($"user-{userId}").SendAsync("ReceiveNotification", notificationDto);
    }
    
}
