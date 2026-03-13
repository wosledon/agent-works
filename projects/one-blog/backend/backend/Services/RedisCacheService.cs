using StackExchange.Redis;
using System.Text.Json;

namespace DotnetBlog.Services;

public class RedisCacheService : ICacheService, IDisposable
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IDatabase _database;
    private readonly ILogger<RedisCacheService> _logger;
    private bool _disposed;

    public RedisCacheService(IConnectionMultiplexer redis, ILogger<RedisCacheService> logger)
    {
        _redis = redis;
        _database = redis.GetDatabase();
        _logger = logger;
    }

    public async Task<T?> GetAsync<T>(string key)
    {
        try
        {
            var value = await _database.StringGetAsync(key);
            
            if (value.IsNullOrEmpty)
            {
                _logger.LogDebug("RedisCache MISS: {Key}", key);
                return default;
            }
            
            _logger.LogDebug("RedisCache HIT: {Key}", key);
            return JsonSerializer.Deserialize<T>(value!);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "RedisCache GET failed for key: {Key}", key);
            return default;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
    {
        try
        {
            var serialized = JsonSerializer.Serialize(value);
            var expiry = expiration ?? TimeSpan.FromMinutes(5);
            
            await _database.StringSetAsync(key, serialized, expiry);
            _logger.LogDebug("RedisCache SET: {Key}, Expiry: {Expiry}", key, expiry);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "RedisCache SET failed for key: {Key}", key);
        }
    }

    public async Task RemoveAsync(string key)
    {
        try
        {
            await _database.KeyDeleteAsync(key);
            _logger.LogDebug("RedisCache REMOVE: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "RedisCache REMOVE failed for key: {Key}", key);
        }
    }

    public async Task<bool> ExistsAsync(string key)
    {
        try
        {
            return await _database.KeyExistsAsync(key);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "RedisCache EXISTS failed for key: {Key}", key);
            return false;
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _redis?.Dispose();
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }
}
