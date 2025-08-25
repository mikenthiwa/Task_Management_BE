namespace Application.Models;

public class Result(bool success)
{
    public bool Success { get; set; } = success;
    public int StatusCode { get; set; }
    public string? Message { get; set; }
    public object? Data { get; set; }
    public IDictionary<string, string[]>? Errors { get; set; }

    public static Result SuccessResponse(int statusCode, string message, object? data = null)
    {
        return new Result(true)
        {
            StatusCode = statusCode,
            Message = message,
            Data = data
        };
    }

    public static Result FailureResponse(int statusCode, string message, IDictionary<string, string[]>? errors = null)
    {
        return new Result(false)
        {
            StatusCode = statusCode,
            Message = message,
            Errors = errors,
        };
    } 
}
