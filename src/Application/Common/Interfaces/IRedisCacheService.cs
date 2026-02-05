namespace Application.Common.Interfaces;

public interface IRedisCacheService
{
    public Task SetAsync<T>(string key, T value, TimeSpan expiry, CancellationToken cancellationToken = default);
    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);
}
