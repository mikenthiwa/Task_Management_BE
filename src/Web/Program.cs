using Infrastructure.Data;
using Infrastructure.Hubs;
using Task_Management_BE.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

var runningInContainer = builder.Configuration.GetValue("DOTNET_RUNNING_IN_CONTAINER", false);

if (builder.Environment.IsDevelopment() && !runningInContainer)
{
    builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);
    builder.Configuration.AddUserSecrets<Program>(optional: true);
}

builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddWebServices(builder.Configuration);


var app = builder.Build();

await app.InitializeDatabaseAsync();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
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
app.UseAuthentication();
app.UseAuthorization();
app.MapEndpoints();
app.MapHub<NotificationHub>("/notificationHub");


app.Run();
