using Microsoft.Extensions.Caching.Memory;

namespace DotnetBlog.Services;

public class MemoryCacheService : ICacheService
{
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<MemoryCacheService> _logger;

    public MemoryCacheService(IMemoryCache memoryCache, ILogger<MemoryCacheService> logger)
    {
        _memoryCache = memoryCache;
        _logger = logger;
    }

    public Task<T?> GetAsync<T>(string key)
    {
        if (_memoryCache.TryGetValue(key, out T? value))
        {
            _logger.LogDebug("MemoryCache HIT: {Key}", key);
            return Task.FromResult(value);
        }
        
        _logger.LogDebug("MemoryCache MISS: {Key}", key);
        return Task.FromResult<T?>(default);
    }

    public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
    {
        var options = new MemoryCacheEntryOptions();
        
        if (expiration.HasValue)
        {
            options.AbsoluteExpirationRelativeToNow = expiration.Value;
        }
        else
        {
            // Default expiration: 5 minutes
            options.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
        }
        
        _memoryCache.Set(key, value, options);
        _logger.LogDebug("MemoryCache SET: {Key}", key);
        
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key)
    {
        _memoryCache.Remove(key);
        _logger.LogDebug("MemoryCache REMOVE: {Key}", key);
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(string key)
    {
        return Task.FromResult(_memoryCache.TryGetValue(key, out _));
    }
}
