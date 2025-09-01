using Application.Models;
using Microsoft.AspNetCore.Identity;

namespace Infrastructure.Identity;

public static class IdentityResultExtensions
{
    public static Result ToApplicationResult(this IdentityResult result)
    {
        return result.Succeeded 
            ? Result.SuccessResponse(201, "User registered successfully") 
            : Result.FailureResponse(400, "User registration failed");
    }
}
