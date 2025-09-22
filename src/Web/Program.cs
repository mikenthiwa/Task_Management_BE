using Infrastructure.Data;
using Task_Management_BE.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

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
app.UseHttpsRedirection();
app.MapEndpoints();
app.UseCors("MyAllowSpecificOrigins");

app.Run();

