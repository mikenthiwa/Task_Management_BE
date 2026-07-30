using System.Reflection;
using Application.Common.Factory;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SharpGrip.FluentValidation.AutoValidation.Endpoints.Extensions;

namespace Application;

public static class DependencyInjection
{
    public static IHostApplicationBuilder AddApplicationServices(this IHostApplicationBuilder builder)
    {
        // Add application services here
        var assembly = Assembly.GetExecutingAssembly();
        builder.Services.AddValidatorsFromAssembly(assembly);
        builder.Services.AddAutoMapper(_ => { }, assembly);
        builder.Services.AddFluentValidationAutoValidation(configuration =>
        {
            configuration.OverrideDefaultResultFactoryWith<CustomResultFactory>();
        });
        builder.Services.AddMediatR(cfg => {
            cfg.RegisterServicesFromAssembly(assembly);
        });

        return builder;
    }
}
