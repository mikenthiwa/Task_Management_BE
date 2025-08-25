using FluentValidation.Results;

namespace Application.Common.Exceptions;

public class ValidationException() : Exception("One or more validation failures have occurred.")
{
    public Dictionary<string, string[]> Errors { get; } = new();
    
    public ValidationException(IEnumerable<ValidationFailure> failures) : this()
    {
        Errors = failures
            .GroupBy(e => e.PropertyName, e => e.ErrorMessage)
            .ToDictionary(failureGroup => failureGroup.Key, failureGroup => failureGroup.ToArray());
    }
}
