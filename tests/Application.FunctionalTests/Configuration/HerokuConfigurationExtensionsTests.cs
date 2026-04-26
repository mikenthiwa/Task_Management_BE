using FluentAssertions;
using Infrastructure.Configuration;
using Microsoft.Extensions.Configuration;

namespace Application.FunctionalTests.Configuration;

public class HerokuConfigurationExtensionsTests
{
    [Fact]
    public void ShouldNotOverwriteStructuredConfiguration()
    {
        var configuration = new ConfigurationManager();
        configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["DATABASE_URL"] = "postgres://heroku-user:heroku-password@heroku-db.example.com:5432/heroku-db",
            ["REDIS_URL"] = "rediss://:heroku-password@heroku-redis.example.com:6380",
            ["CLOUDAMQP_URL"] = "amqps://heroku-user:heroku-password@heroku-rabbit.example.com:5671/heroku-vhost",
            ["ConnectionStrings:DefaultConnection"] = "Host=structured-db;Port=5432;Database=structured;Username=app;Password=secret",
            ["Caching:Redis:ConnectionString"] = "structured-redis:6379,password=secret,ssl=false",
            ["Caching:Redis:SkipCertificateValidation"] = "False",
            ["RabbitMq:HostName"] = "structured-rabbit",
            ["RabbitMq:UserName"] = "structured-user",
            ["RabbitMq:Password"] = "structured-password",
            ["RabbitMq:VirtualHost"] = "structured-vhost",
            ["RabbitMq:Port"] = "5672",
            ["RabbitMq:UseSsl"] = "False"
        });

        configuration.AddHerokuAddonConfiguration();

        configuration.GetConnectionString("DefaultConnection")
            .Should().Be("Host=structured-db;Port=5432;Database=structured;Username=app;Password=secret");
        configuration["Caching:Redis:ConnectionString"]
            .Should().Be("structured-redis:6379,password=secret,ssl=false");
        configuration["RabbitMq:HostName"].Should().Be("structured-rabbit");
        configuration["RabbitMq:UseSsl"].Should().Be("False");
    }

    [Fact]
    public void ShouldConvertHerokuAddonUrlsWhenStructuredConfigurationIsMissing()
    {
        var configuration = new ConfigurationManager();
        configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["DATABASE_URL"] = "postgres://db-user:db-password@db.example.com:5432/taskdb",
            ["REDIS_URL"] = "rediss://:redis-password@redis.example.com:6380",
            ["CLOUDAMQP_URL"] = "amqps://rabbit-user:rabbit-password@rabbit.example.com:5671/rabbit-vhost"
        });

        configuration.AddHerokuAddonConfiguration();

        configuration.GetConnectionString("DefaultConnection")
            .Should().Be("Host=db.example.com;Port=5432;Database=taskdb;Username=db-user;Password=db-password;SSL Mode=Require;Trust Server Certificate=true");
        configuration["Caching:Redis:ConnectionString"]
            .Should().Be("redis.example.com:6380,password=redis-password,ssl=true,sslProtocols=tls12,abortConnect=false");
        configuration["Caching:Redis:SkipCertificateValidation"].Should().Be("True");
        configuration["RabbitMq:HostName"].Should().Be("rabbit.example.com");
        configuration["RabbitMq:UserName"].Should().Be("rabbit-user");
        configuration["RabbitMq:Password"].Should().Be("rabbit-password");
        configuration["RabbitMq:VirtualHost"].Should().Be("rabbit-vhost");
        configuration["RabbitMq:Port"].Should().Be("5671");
        configuration["RabbitMq:UseSsl"].Should().Be("True");
    }
}
