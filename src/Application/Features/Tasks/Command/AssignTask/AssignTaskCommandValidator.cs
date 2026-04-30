using FluentValidation;

namespace Application.Features.Tasks.Command.AssignTask;

public class AssignTaskCommandValidator : AbstractValidator<AssignTaskCommand>
{
    public AssignTaskCommandValidator()
    {
        RuleFor(v => v.AssignedId)
            .NotEmpty()
            .WithMessage("AssignedId is required");

        RuleFor(v => v.RowVersion)
            .GreaterThan(0U)
            .WithMessage("RowVersion is required");
    }
}
