using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using OneReport.Services.Interfaces;

namespace OneReport.Services.Implementations;

/// <summary>
/// 内存缓存服务实现
/// </summary>
public class MemoryCacheService : ICacheService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<MemoryCacheService> _logger;

    public MemoryCacheService(IMemoryCache cache, ILogger<MemoryCacheService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue(key, out T? value))
        {
            _logger.LogDebug("Memory cache hit: {Key}", key);
            return Task.FromResult(value);
        }
        
        _logger.LogDebug("Memory cache miss: {Key}", key);
        return Task.FromResult(default(T));
    }

    public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
    {
        var options = new MemoryCacheEntryOptions();
        
        if (expiration.HasValue)
        {
            options.AbsoluteExpirationRelativeToNow = expiration.Value;
        }
        else
        {
            // 默认1小时过期
            options.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
        }
        
        _cache.Set(key, value, options);
        _logger.LogDebug("Memory cache set: {Key}, expires in {Expiration}", key, expiration ?? TimeSpan.FromHours(1));
        
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        _cache.Remove(key);
        _logger.LogDebug("Memory cache removed: {Key}", key);
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_cache.TryGetValue(key, out _));
    }
}
