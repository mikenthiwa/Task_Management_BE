using Application.Common.Interfaces;
using MediatR;

namespace Application.Features.Notifications.command.MarkAllAsRead;

public record MarkAllNotificationAsReadCommand : IRequest
{
    public required string UserId { get; init; }
}

public class MarkAllNotificationAsRead(INotificationService notificationService, ICurrentUserService currentUserService) : IRequestHandler<MarkAllNotificationAsReadCommand>
{
    public async Task Handle(MarkAllNotificationAsReadCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;
        await notificationService.MarkAllNotificationsAsReadAsync(userId!);
    }
}
