using FluentValidation;

namespace Application.Features.Notifications.command.MarkAllAsRead;

public class MarkAllNotificationAsReadCommandValidator:AbstractValidator<MarkAllNotificationAsReadCommand>
{
    public MarkAllNotificationAsReadCommandValidator()
    {
        RuleFor(v => v.UserId)
            .NotEmpty();
    }
}
