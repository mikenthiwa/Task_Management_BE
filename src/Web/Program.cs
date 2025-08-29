using Infrastructure.Data;
using Infrastructure.Identity;

var builder = WebApplication.CreateBuilder(args);

builder.AddApplicationServices();
builder.AddInfrastructureServices();
builder.AddWebServices();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    await app.InitializeDatabaseAsync();
    app.MapOpenApi();
}

app.MapIdentityApi<ApplicationUser>();
app.UseHttpsRedirection();
app.UseExceptionHandler(options => { });

app.Run();

