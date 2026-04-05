using System.Text.Json;
using OrderProcessingService.Application.Caching;
using StackExchange.Redis;

namespace OrderProcessingService.Infrastructure.Caching;

public sealed class RedisCacheService : ICacheService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IConnectionMultiplexer _mux;
    private readonly IDatabase _db;

    public RedisCacheService(IConnectionMultiplexer multiplexer)
    {
        _mux = multiplexer;
        _db = multiplexer.GetDatabase();
    }

    public async Task<T?> GetAsync<T>(string key)
    {
        try
        {
            var value = await _db.StringGetAsync(key).ConfigureAwait(false);
            if (!value.HasValue)
                return default;

            return JsonSerializer.Deserialize<T>(value.ToString()!, JsonOptions);
        }
        catch
        {
            return default;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan ttl)
    {
        try
        {
            var json = JsonSerializer.Serialize(value, JsonOptions);
            await _db.StringSetAsync(key, json, ttl).ConfigureAwait(false);
        }
        catch
        {
            // Cache is best-effort; failures must not break callers.
        }
    }

    public async Task DeleteAsync(string key)
    {
        try
        {
            await _db.KeyDeleteAsync(key).ConfigureAwait(false);
        }
        catch
        {
        }
    }

    public async Task DeleteByPatternAsync(string pattern)
    {
        try
        {
            if (string.IsNullOrEmpty(pattern))
                return;

            foreach (var endpoint in _mux.GetEndPoints())
            {
                var server = _mux.GetServer(endpoint);
                if (!server.IsConnected)
                    continue;

                RedisKey[] keys;
                try
                {
                    keys = server.Keys(database: _db.Database, pattern: pattern).ToArray();
                }
                catch
                {
                    continue;
                }

                foreach (var key in keys)
                {
                    try
                    {
                        await _db.KeyDeleteAsync(key).ConfigureAwait(false);
                    }
                    catch
                    {
                        // Ignore per-key failures (e.g. transient connection loss).
                    }
                }
            }
        }
        catch
        {
        }
    }
}
