using OneReport.Services.Interfaces;

namespace OneReport.Services.Background;

/// <summary>
/// 缓存清理后台服务 - 定期清理过期缓存
/// </summary>
public class CacheCleanupBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<CacheCleanupBackgroundService> _logger;
    private readonly TimeSpan _cleanupInterval;

    public CacheCleanupBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<CacheCleanupBackgroundService> logger,
        IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _cleanupInterval = TimeSpan.FromHours(
            configuration.GetValue<double>("Cache:CleanupIntervalHours", 1));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("缓存清理服务启动，清理间隔: {Interval}", _cleanupInterval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_cleanupInterval, stoppingToken);
                await CleanupExpiredCachesAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "缓存清理过程中发生错误");
            }
        }

        _logger.LogInformation("缓存清理服务停止");
    }

    private async Task CleanupExpiredCachesAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var cacheService = scope.ServiceProvider.GetRequiredService<IQueryResultCacheService>();
            
            var count = await cacheService.CleanupExpiredAsync(cancellationToken);
            
            if (count > 0)
            {
                _logger.LogInformation("后台清理了 {Count} 条过期缓存", count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "后台清理缓存失败");
        }
    }
}