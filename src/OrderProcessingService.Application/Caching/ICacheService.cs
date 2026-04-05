namespace OrderProcessingService.Application.Caching;

public interface ICacheService
{
    Task<T?> GetAsync<T>(string key);

    Task SetAsync<T>(string key, T value, TimeSpan ttl);

    Task DeleteAsync(string key);
}
