using FluentValidation;

namespace Application.Features.Tasks.Command.UpdateTaskStatus;

public class UpdateTaskStatusCommandValidator : AbstractValidator<UpdateTaskStatusCommand>
{
    public UpdateTaskStatusCommandValidator()
    {
        RuleFor(v => v.Status)
            .IsInEnum()
            .WithMessage("Invalid status value");
    }
}
