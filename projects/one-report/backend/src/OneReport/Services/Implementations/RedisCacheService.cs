using System.Text.Json;
using StackExchange.Redis;
using OneReport.Services.Interfaces;

namespace OneReport.Services.Implementations;

/// <summary>
/// Redis 缓存服务实现
/// </summary>
public class RedisCacheService : ICacheService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IDatabase _db;
    private readonly ILogger<RedisCacheService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public RedisCacheService(IConnectionMultiplexer redis, ILogger<RedisCacheService> logger)
    {
        _redis = redis;
        _db = redis.GetDatabase();
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var value = await _db.StringGetAsync(key);
            
            if (value.IsNullOrEmpty)
            {
                _logger.LogDebug("Redis cache miss: {Key}", key);
                return default;
            }

            _logger.LogDebug("Redis cache hit: {Key}", key);
            return JsonSerializer.Deserialize<T>(value!, _jsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis get failed for key: {Key}", key);
            return default;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var serialized = JsonSerializer.Serialize(value, _jsonOptions);
            var expiry = expiration ?? TimeSpan.FromHours(1);
            
            await _db.StringSetAsync(key, serialized, expiry);
            _logger.LogDebug("Redis cache set: {Key}, expires in {Expiration}", key, expiry);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis set failed for key: {Key}", key);
        }
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            await _db.KeyDeleteAsync(key);
            _logger.LogDebug("Redis cache removed: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis remove failed for key: {Key}", key);
        }
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _db.KeyExistsAsync(key);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis exists check failed for key: {Key}", key);
            return false;
        }
    }
}
