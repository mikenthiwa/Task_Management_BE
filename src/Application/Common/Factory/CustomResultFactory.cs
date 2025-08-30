using Microsoft.AspNetCore.Http;
using SharpGrip.FluentValidation.AutoValidation.Endpoints.Results;


namespace Application.Common.Factory;

public class CustomResultFactory : IFluentValidationAutoValidationResultFactory
{
    public IResult CreateResult(EndpointFilterInvocationContext context, FluentValidation.Results.ValidationResult validationResult)
    {
        throw new Exceptions.ValidationException(validationResult.Errors);
    }
}
