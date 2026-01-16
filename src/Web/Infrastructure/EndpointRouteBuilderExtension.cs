using System.Diagnostics.CodeAnalysis;

namespace Task_Management_BE.Infrastructure;

public static class EndpointRouteBuilderExtension
{
    public static IEndpointRouteBuilder MapGet(this IEndpointRouteBuilder builder, Delegate handler, [StringSyntax("Route")] string pattern = "")
    {
        builder.MapGet(pattern, handler)
            .WithName(handler.Method.Name);

        return builder;
    }
    
    public static IEndpointRouteBuilder MapPost(this IEndpointRouteBuilder builder, Delegate handler, [StringSyntax("Route")] string pattern = "")
    {
        builder.MapPost(pattern, handler)
            .WithName(handler.Method.Name);

        return builder;
    }
    
    public static IEndpointRouteBuilder MapPut(this IEndpointRouteBuilder builder, Delegate handler, [StringSyntax("Route")] string pattern)
    {
        builder.MapPut(pattern, handler)
            .WithName(handler.Method.Name);

        return builder;
    }
    
    public static IEndpointRouteBuilder MapDelete(this IEndpointRouteBuilder builder, Delegate handler, [StringSyntax("Route")] string pattern)
    {
        builder.MapDelete(pattern, handler)
            .WithName(handler.Method.Name);

        return builder;
    }
    
    public static IEndpointRouteBuilder MapPatch(this IEndpointRouteBuilder builder, Delegate handler, [StringSyntax("Route")] string pattern)
    {
        builder.MapPatch(pattern, handler)
            .WithName(handler.Method.Name);

        return builder;
    }
}
