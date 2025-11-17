using FluentValidation;

namespace Application.Features.Auth.Command.Oauth;

public class OauthCommandValidator: AbstractValidator<OauthCommand>
{
    public OauthCommandValidator()
    {
        RuleFor(v => v.Username)
            .NotEmpty();
        
        RuleFor(v => v.Email)
            .NotEmpty();
    }
}
