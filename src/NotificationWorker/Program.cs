
using System.Reflection;
using Infrastructure.Configuration;
using Infrastructure.RabbitMq;
using Application.Common.Interfaces;
using Application.Common.Options;
using NotificationWorker;

var builder = Host.CreateApplicationBuilder(args);
var assembly = Assembly.GetExecutingAssembly();

builder.Configuration.AddHerokuAddonConfiguration();

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.Configure<NotificationConsumerOptions>(
    builder.Configuration.GetSection(NotificationConsumerOptions.SectionName));

builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));
builder.Services.AddAutoMapper(_ => { }, assembly);
builder.Services.AddHttpClient("web", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["WebBaseUrl"]!);
    client.DefaultRequestHeaders.Add("X-Worker-Key", builder.Configuration["WorkerApiKey"]!);
});
builder.Services.AddSingleton<IMessageBus>(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    return new RabbitMqMessageBus(
        configuration["RabbitMq:HostName"] ?? "localhost",
        configuration["RabbitMq:UserName"] ?? "admin",
        configuration["RabbitMq:Password"] ?? "admin",
        configuration["RabbitMq:VirtualHost"] ?? "/",
        configuration.GetValue<int?>("RabbitMq:Port") ?? 5672,
        configuration.GetValue<bool>("RabbitMq:UseSsl"));
});
builder.Services.AddSingleton<INotificationDeliveryProcessor, NotificationDeliveryProcessor>();
builder.Services.AddSingleton<NotificationDeliveryHandler>();
builder.Services.AddHostedService(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var hostName = configuration["RabbitMq:HostName"] ?? "localhost";
    var userName = configuration["RabbitMq:UserName"] ?? "admin";
    var password = configuration["RabbitMq:Password"] ?? "admin";
    var virtualHost = configuration["RabbitMq:VirtualHost"] ?? "/";
    var port = configuration.GetValue<int?>("RabbitMq:Port") ?? 5672;
    var useSsl = configuration.GetValue<bool>("RabbitMq:UseSsl");
    return new Worker(
        sp.GetRequiredService<NotificationDeliveryHandler>(),
        sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<NotificationConsumerOptions>>(),
        sp.GetRequiredService<ILogger<Worker>>(),
        hostName,
        userName,
        password,
        virtualHost,
        port,
        useSsl);
});

var host = builder.Build();
host.Run();
