using Web.Infrastructure;

namespace Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    public static void AddWebServices(this IHostApplicationBuilder builder)
    {
        builder.Services.AddOpenApi();
        builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
    }
}
