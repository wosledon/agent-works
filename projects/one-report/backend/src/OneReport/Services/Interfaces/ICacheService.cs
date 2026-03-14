namespace OneReport.Services.Interfaces;

/// <summary>
/// 缓存服务接口
/// </summary>
public interface ICacheService
{
    /// <summary>
    /// 获取缓存值
    /// </summary>
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 设置缓存值
    /// </summary>
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 删除缓存值
    /// </summary>
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 检查缓存键是否存在
    /// </summary>
    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);
}
