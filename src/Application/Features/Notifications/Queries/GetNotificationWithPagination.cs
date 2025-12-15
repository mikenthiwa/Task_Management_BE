using Application.Common.Interfaces;
using Application.Common.Mappings;
using Application.Common.Models;
using AutoMapper.QueryableExtensions;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Application.Features.Notifications.Queries;

public record GetNotificationWithPaginationQuery : IRequest<PaginatedList<NotificationDto>>
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public required string UserId { get; init; }
}

public class GetNotificationWithPagination(INotificationService notificationService, IMapper mapper, ICurrentUserService currentUserService) : IRequestHandler<GetNotificationWithPaginationQuery, PaginatedList<NotificationDto>>
{
    public async Task<PaginatedList<NotificationDto>> Handle(GetNotificationWithPaginationQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;
        var query = notificationService.GetUserNotificationsAsync(request.UserId);

        return await query
            .ProjectTo<NotificationDto>(mapper.ConfigurationProvider)
            .PaginatedListAsync(request.PageNumber, request.PageSize);
    }
    
    
}
