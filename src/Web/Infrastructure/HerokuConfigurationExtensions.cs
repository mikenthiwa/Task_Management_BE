using Microsoft.Extensions.Configuration;

namespace Task_Management_BE.Infrastructure;

public static class HerokuConfigurationExtensions
{
    public static ConfigurationManager AddHerokuAddonConfiguration(this ConfigurationManager configuration)
    {
        var values = new Dictionary<string, string?>();

        AddPostgresConfiguration(configuration, values);
        AddRedisConfiguration(configuration, values);
        AddRabbitMqConfiguration(configuration, values);

        if (values.Count > 0)
        {
            configuration.AddInMemoryCollection(values);
        }

        return configuration;
    }

    private static void AddPostgresConfiguration(IConfiguration configuration, Dictionary<string, string?> values)
    {
        var databaseUrl = configuration["DATABASE_URL"];
        var currentConnectionString = configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(databaseUrl) || IsNpgsqlConnectionString(currentConnectionString))
        {
            return;
        }

        var uri = new Uri(databaseUrl);
        var userInfo = uri.UserInfo.Split(':', 2);
        var username = Uri.UnescapeDataString(userInfo[0]);
        var password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : string.Empty;
        var database = Uri.UnescapeDataString(uri.AbsolutePath.TrimStart('/'));

        values["ConnectionStrings:DefaultConnection"] =
            $"Host={uri.Host};Port={uri.Port};Database={database};Username={username};Password={password};SSL Mode=Require;Trust Server Certificate=true";
    }

    private static void AddRedisConfiguration(IConfiguration configuration, Dictionary<string, string?> values)
    {
        var redisUrl = configuration["REDIS_URL"];
        var currentConnectionString = configuration["Caching:Redis:ConnectionString"];

        if (string.IsNullOrWhiteSpace(redisUrl) || !IsUrlConnectionString(currentConnectionString))
        {
            return;
        }

        var uri = new Uri(redisUrl);
        var userInfo = uri.UserInfo.Split(':', 2);
        var password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : string.Empty;
        var ssl = uri.Scheme.Equals("rediss", StringComparison.OrdinalIgnoreCase);

        values["Caching:Redis:ConnectionString"] =
            $"{uri.Host}:{uri.Port},password={password},ssl={ssl.ToString().ToLowerInvariant()},sslProtocols=tls12,abortConnect=false";
        values["Caching:Redis:SkipCertificateValidation"] = ssl.ToString();
    }

    private static void AddRabbitMqConfiguration(IConfiguration configuration, Dictionary<string, string?> values)
    {
        var cloudAmqpUrl = configuration["CLOUDAMQP_URL"];

        if (string.IsNullOrWhiteSpace(cloudAmqpUrl) || !string.IsNullOrWhiteSpace(configuration["RabbitMq:HostName"]))
        {
            return;
        }

        var uri = new Uri(cloudAmqpUrl);
        var userInfo = uri.UserInfo.Split(':', 2);
        var username = Uri.UnescapeDataString(userInfo[0]);
        var password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : string.Empty;
        var virtualHost = Uri.UnescapeDataString(uri.AbsolutePath.TrimStart('/'));
        var useSsl = uri.Scheme.Equals("amqps", StringComparison.OrdinalIgnoreCase);
        var port = uri.Port > 0 ? uri.Port : useSsl ? 5671 : 5672;

        values["RabbitMq:HostName"] = uri.Host;
        values["RabbitMq:UserName"] = username;
        values["RabbitMq:Password"] = password;
        values["RabbitMq:VirtualHost"] = string.IsNullOrWhiteSpace(virtualHost) ? "/" : virtualHost;
        values["RabbitMq:Port"] = port.ToString();
        values["RabbitMq:UseSsl"] = useSsl.ToString();
    }

    private static bool IsNpgsqlConnectionString(string? connectionString)
    {
        return !string.IsNullOrWhiteSpace(connectionString)
               && !IsUrlConnectionString(connectionString);
    }

    private static bool IsUrlConnectionString(string? connectionString)
    {
        return string.IsNullOrWhiteSpace(connectionString)
               || connectionString.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase)
               || connectionString.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase)
               || connectionString.StartsWith("redis://", StringComparison.OrdinalIgnoreCase)
               || connectionString.StartsWith("rediss://", StringComparison.OrdinalIgnoreCase);
    }
}
