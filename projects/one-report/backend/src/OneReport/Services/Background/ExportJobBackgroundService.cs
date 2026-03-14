using System.Threading.Channels;
using OneReport.Services.Interfaces;

namespace OneReport.Services.Background;

/// <summary>
/// 导出任务后台处理器 - 使用 Channel 实现高效的任务队列处理
/// </summary>
public class ExportJobBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ExportJobBackgroundService> _logger;
    private readonly Channel<ExportJobRequest> _jobChannel;
    
    // 并发限制
    private const int MaxConcurrentExports = 3;
    private readonly SemaphoreSlim _semaphore;

    public ExportJobBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<ExportJobBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _semaphore = new SemaphoreSlim(MaxConcurrentExports, MaxConcurrentExports);
        _jobChannel = Channel.CreateUnbounded<ExportJobRequest>(new UnboundedChannelOptions
        {
            SingleReader = false,
            SingleWriter = false
        });
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("导出任务后台处理器已启动");

        // 启动多个消费者
        var consumers = Enumerable.Range(0, MaxConcurrentExports)
            .Select(_ => ProcessJobsAsync(stoppingToken))
            .ToList();

        // 启动任务生产者
        var producer = PollJobsAsync(stoppingToken);

        await Task.WhenAll(consumers.Concat(new[] { producer }));
    }

    /// <summary>
    /// 从数据库轮询待处理任务
    /// </summary>
    private async Task PollJobsAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var queueService = scope.ServiceProvider.GetRequiredService<IExportJobQueueService>();

                var pendingCount = await queueService.GetPendingCountAsync(cancellationToken);
                
                if (pendingCount > 0)
                {
                    var job = await queueService.DequeueAsync(cancellationToken);
                    
                    if (job != null)
                    {
                        await _jobChannel.Writer.WriteAsync(job, cancellationToken);
                        _logger.LogDebug("任务已加入处理通道: {JobId}", job.JobId);
                    }
                }

                // 轮询间隔
                await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "轮询任务时发生错误");
                await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
            }
        }
    }

    /// <summary>
    /// 处理队列中的任务
    /// </summary>
    private async Task ProcessJobsAsync(CancellationToken cancellationToken)
    {
        await foreach (var job in _jobChannel.Reader.ReadAllAsync(cancellationToken))
        {
            await _semaphore.WaitAsync(cancellationToken);
            
            _ = Task.Run(async () =>
            {
                try
                {
                    await ExecuteJobAsync(job, cancellationToken);
                }
                finally
                {
                    _semaphore.Release();
                }
            }, cancellationToken);
        }
    }

    /// <summary>
    /// 执行单个导出任务
    /// </summary>
    private async Task ExecuteJobAsync(ExportJobRequest job, CancellationToken cancellationToken)
    {
        _logger.LogInformation("开始执行导出任务: {JobId}, 格式: {Format}", job.JobId, job.Format);
        
        using var scope = _serviceProvider.CreateScope();
        var exportService = scope.ServiceProvider.GetRequiredService<IReportExportService>();
        var queueService = scope.ServiceProvider.GetRequiredService<IExportJobQueueService>();
        var environment = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var status = new ExportJobStatus
        {
            JobId = job.JobId,
            Status = "processing",
            StartedAt = DateTime.UtcNow
        };

        Stream? exportStream = null;
        
        try
        {
            // 执行导出
            exportStream = job.Format.ToLower() switch
            {
                "csv" => await exportService.ExportToCsvAsync(job.ReportDefinitionId, job.Parameters, cancellationToken),
                "excel" or "xlsx" => await exportService.ExportToExcelAsync(job.ReportDefinitionId, job.Parameters, cancellationToken),
                "json" => await exportService.ExportToJsonAsync(job.ReportDefinitionId, job.Parameters, cancellationToken),
                "pdf" => await exportService.ExportToPdfAsync(job.ReportDefinitionId, job.Parameters, cancellationToken),
                _ => throw new NotSupportedException($"不支持的导出格式: {job.Format}")
            };

            // 保存到文件
            var fileName = $"{job.FileName ?? "export"}_{job.JobId:N}.{GetFileExtension(job.Format)}";
            var uploadsPath = Path.Combine(environment.WebRootPath ?? "wwwroot", "exports");
            Directory.CreateDirectory(uploadsPath);
            var filePath = Path.Combine(uploadsPath, fileName);

            using (var fileStream = File.Create(filePath))
            {
                await exportStream.CopyToAsync(fileStream, cancellationToken);
            }

            var fileInfo = new FileInfo(filePath);
            stopwatch.Stop();

            // 更新状态为完成
            status.Status = "completed";
            status.FilePath = filePath;
            status.FileSize = fileInfo.Length;
            status.CompletedAt = DateTime.UtcNow;

            await queueService.UpdateStatusAsync(job.JobId, status, cancellationToken: cancellationToken);

            _logger.LogInformation(
                "导出任务完成: {JobId}, 文件大小: {FileSize} bytes, 耗时: {ElapsedMs}ms",
                job.JobId, fileInfo.Length, stopwatch.ElapsedMilliseconds);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("导出任务被取消: {JobId}", job.JobId);
            
            status.Status = "cancelled";
            status.ErrorMessage = "任务被取消";
            status.CompletedAt = DateTime.UtcNow;
            
            await queueService.UpdateStatusAsync(job.JobId, status, cancellationToken: CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "导出任务失败: {JobId}", job.JobId);
            
            status.Status = "failed";
            status.ErrorMessage = ex.Message;
            status.CompletedAt = DateTime.UtcNow;
            
            await queueService.UpdateStatusAsync(job.JobId, status, ex.Message, CancellationToken.None);
        }
        finally
        {
            exportStream?.Dispose();
        }
    }

    private static string GetFileExtension(string format)
    {
        return format.ToLower() switch
        {
            "csv" => "csv",
            "excel" or "xlsx" => "xlsx",
            "json" => "json",
            "pdf" => "pdf",
            _ => "txt"
        };
    }
}
