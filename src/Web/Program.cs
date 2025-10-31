using Infrastructure.Data;
using Task_Management_BE.Infrastructure;
using Microsoft.Extensions.Configuration;

var builder = WebApplication.CreateBuilder(args);

var runningInContainer = builder.Configuration.GetValue("DOTNET_RUNNING_IN_CONTAINER", false);

if (builder.Environment.IsDevelopment() && !runningInContainer)
{
    builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);
    builder.Configuration.AddUserSecrets<Program>(optional: true);
}

builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddWebServices();


var app = builder.Build();


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    await app.InitializeDatabaseAsync();
    app.MapOpenApi();
    app.UseOpenApi();
}

app.UseSwaggerUi(settings =>
{
    settings.Path = "/api";
});


app.UseExceptionHandler(options => { });
// app.UseHttpsRedirection();
app.UseCors("MyAllowSpecificOrigins");
app.MapEndpoints();

app.Run();
