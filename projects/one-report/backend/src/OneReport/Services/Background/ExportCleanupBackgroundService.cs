using OneReport.Services.Interfaces;

namespace OneReport.Services.Background;

/// <summary>
/// 导出文件清理后台服务 - 定期清理过期的导出文件
/// </summary>
public class ExportCleanupBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ExportCleanupBackgroundService> _logger;
    private readonly TimeSpan _cleanupInterval = TimeSpan.FromHours(6);
    private readonly TimeSpan _fileRetentionPeriod = TimeSpan.FromDays(7);

    public ExportCleanupBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<ExportCleanupBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("导出文件清理服务已启动，清理间隔: {Interval}, 文件保留期: {Retention}",
            _cleanupInterval, _fileRetentionPeriod);

        // 启动时立即执行一次清理
        await RunCleanupAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_cleanupInterval, stoppingToken);
                
                if (!stoppingToken.IsCancellationRequested)
                {
                    await RunCleanupAsync(stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "清理任务执行失败");
            }
        }

        _logger.LogInformation("导出文件清理服务已停止");
    }

    private async Task RunCleanupAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var queueService = scope.ServiceProvider.GetRequiredService<IExportJobQueueService>();

            await queueService.CleanupAsync(_fileRetentionPeriod, cancellationToken);
            
            _logger.LogInformation("定期清理任务完成");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "定期清理任务失败");
        }
    }
}
