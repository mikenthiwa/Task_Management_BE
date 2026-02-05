using Application.Common.Interfaces;
using StackExchange.Redis;
using System.Text.Json;

namespace Infrastructure.Redis;

public class RedisCacheService(IConnectionMultiplexer connection) : IRedisCacheService
{
    private readonly IDatabase _database = connection.GetDatabase();
    
    public async Task SetAsync<T>(string key, T value, TimeSpan expiry, CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(value);
        await _database.StringSetAsync(key, json, expiry);
    }
    
    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        var json = await _database.StringGetAsync(key);
        if (json.IsNullOrEmpty)
        {
            return default;
        }
        return JsonSerializer.Deserialize<T>((string)json!);
    }
    
}
