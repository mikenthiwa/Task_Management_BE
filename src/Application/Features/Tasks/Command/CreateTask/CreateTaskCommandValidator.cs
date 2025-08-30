using FluentValidation;

namespace Application.Features.Tasks.Command.CreateTask;

public class CreateTaskCommandValidator : AbstractValidator<CreateTaskCommand>
{
    public CreateTaskCommandValidator()
    {
        RuleFor(v => v.Title)
            .MaximumLength(200)
            .NotEmpty();
        
        RuleFor(v => v.Description)
            .MaximumLength(1000)
            .NotEmpty();
    }
}
