
using System.Reflection;
using Infrastructure;
using Infrastructure.Configuration;
using NotificationWorker;

var builder = Host.CreateApplicationBuilder(args);
var assembly = Assembly.GetExecutingAssembly();

builder.Configuration.AddHerokuAddonConfiguration();

builder.AddInfrastructureServices();

builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));
builder.Services.AddAutoMapper(_ => { }, assembly);
builder.Services.AddHttpClient("web", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["WebBaseUrl"]!);
    client.DefaultRequestHeaders.Add("X-Worker-Key", builder.Configuration["WorkerApiKey"]!);
});
builder.Services.AddHostedService(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var hostName = configuration["RabbitMq:HostName"] ?? "localhost";
    var userName = configuration["RabbitMq:UserName"] ?? "admin";
    var password = configuration["RabbitMq:Password"] ?? "admin";
    var virtualHost = configuration["RabbitMq:VirtualHost"] ?? "/";
    var port = configuration.GetValue<int?>("RabbitMq:Port") ?? 5672;
    var useSsl = configuration.GetValue<bool>("RabbitMq:UseSsl");
    return new Worker(sp, hostName, userName, password, virtualHost, port, useSsl);
});

var host = builder.Build();
host.Run();
