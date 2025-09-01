namespace Application.Models;


public class Result
{
    public bool Success { get; set; }
    public int StatusCode { get; set; }
    public string? Message { get; set; }
    public IDictionary<string, string[]>? Errors { get; set; }
    
    public static Result SuccessResponse(int statusCode, string message)
    {
        return new Result {Success = true, StatusCode = statusCode, Message = message};
    }

    public static Result FailureResponse(int statusCode, string message, IDictionary<string, string[]>? errors = null)
    {
        return new Result { Success = false, StatusCode = statusCode, Message = message, Errors = errors };
    }
}

public class Result<T> : Result
{
    public T? Data { get; set; }

    public static Result<T> SuccessResponse(int statusCode, string message, T data)
    {
        return new Result<T>() { Success = true, StatusCode = statusCode, Message = message, Data = data };
    }
}
