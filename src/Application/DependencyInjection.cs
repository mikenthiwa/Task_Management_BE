using Microsoft.Extensions.Hosting;
using SharpGrip.FluentValidation.AutoValidation.Endpoints.Extensions;

namespace Application;

public static class DependencyInjection
{
    public static void AddApplicationServices(this IHostApplicationBuilder builder)
    {
        // Add application services here
        builder.Services.AddFluentValidationAutoValidation();
    }
}
