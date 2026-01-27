
using System.Reflection;
using Infrastructure;
using NotificationWorker;

var builder = Host.CreateApplicationBuilder(args);
var assembly = Assembly.GetExecutingAssembly();

builder.Services.AddInfrastructureServices(builder.Configuration);

builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));
builder.Services.AddAutoMapper(assembly);
builder.Services.AddHttpClient("web", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["WebBaseUrl"]!);
    client.DefaultRequestHeaders.Add("X-Worker-Key", builder.Configuration["WorkerApiKey"]!);
});
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
