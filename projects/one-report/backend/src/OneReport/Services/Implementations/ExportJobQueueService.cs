using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OneReport.Data;
using OneReport.Services.Interfaces;

namespace OneReport.Services.Implementations;

/// <summary>
/// 导出任务队列服务实现 - 基于数据库 + 内存队列的混合模式
/// </summary>
public class ExportJobQueueService : IExportJobQueueService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ExportJobQueueService> _logger;
    
    // 内存中的优先级队列
    private readonly ConcurrentPriorityQueue<ExportJobRequest, int> _memoryQueue = new();
    
    // 状态缓存
    private readonly ConcurrentDictionary<Guid, ExportJobStatus> _statusCache = new();

    public ExportJobQueueService(
        IServiceProvider serviceProvider,
        ILogger<ExportJobQueueService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task<Guid> EnqueueAsync(ExportJobRequest request, CancellationToken cancellationToken = default)
    {
        // 添加到数据库
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var history = new Data.Entities.ReportExportHistory
        {
            Id = request.JobId,
            ReportDefinitionId = request.ReportDefinitionId,
            ExportFormat = request.Format,
            Status = "pending",
            CreatedAt = request.CreatedAt,
            Parameters = request.Parameters != null ? JsonSerializer.Serialize(request.Parameters) : null
        };

        dbContext.ReportExportHistories.Add(history);
        await dbContext.SaveChangesAsync(cancellationToken);

        // 添加到内存队列（优先级高的在前面）
        var priority = (int)request.Priority * -1; // 负数使高优先级排在前面
        _memoryQueue.Enqueue(request, priority);

        // 更新缓存
        _statusCache[request.JobId] = new ExportJobStatus
        {
            JobId = request.JobId,
            Status = "pending",
            CreatedAt = request.CreatedAt
        };

        _logger.LogInformation("导出任务已加入队列: {JobId}, 优先级: {Priority}", request.JobId, request.Priority);
        
        return request.JobId;
    }

    public async Task<ExportJobRequest?> DequeueAsync(CancellationToken cancellationToken = default)
    {
        // 先从内存队列获取
        if (_memoryQueue.TryDequeue(out var request))
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // 更新数据库状态为处理中
            var history = await dbContext.ReportExportHistories.FindAsync(
                new object[] { request.JobId }, cancellationToken);
            
            if (history != null && history.Status == "pending")
            {
                history.Status = "processing";
                await dbContext.SaveChangesAsync(cancellationToken);

                // 更新缓存
                if (_statusCache.TryGetValue(request.JobId, out var status))
                {
                    status.Status = "processing";
                    status.StartedAt = DateTime.UtcNow;
                }

                _logger.LogInformation("导出任务开始处理: {JobId}", request.JobId);
                return request;
            }
        }

        // 内存队列为空时，从数据库加载待处理任务
        using (var scope = _serviceProvider.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            
            var pendingHistory = await dbContext.ReportExportHistories
                .AsNoTracking()
                .Where(h => h.Status == "pending")
                .OrderBy(h => h.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);

            if (pendingHistory != null)
            {
                // 更新状态为处理中
                pendingHistory.Status = "processing";
                await dbContext.SaveChangesAsync(cancellationToken);

                return new ExportJobRequest
                {
                    JobId = pendingHistory.Id,
                    ReportDefinitionId = pendingHistory.ReportDefinitionId,
                    Format = pendingHistory.ExportFormat,
                    Parameters = pendingHistory.Parameters != null 
                        ? JsonSerializer.Deserialize<Dictionary<string, object?>>(pendingHistory.Parameters)
                        : null,
                    CreatedAt = pendingHistory.CreatedAt
                };
            }
        }

        return null;
    }

    public async Task UpdateStatusAsync(Guid jobId, ExportJobStatus status, string? message = null, CancellationToken cancellationToken = default)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var history = await dbContext.ReportExportHistories.FindAsync(
            new object[] { jobId }, cancellationToken);

        if (history != null)
        {
            history.Status = status.Status;
            history.RecordCount = status.RecordCount;
            history.FileSize = status.FileSize;
            history.FilePath = status.FilePath;
            history.ErrorMessage = message ?? status.ErrorMessage;
            history.CompletedAt = status.CompletedAt;

            await dbContext.SaveChangesAsync(cancellationToken);
        }

        // 更新缓存
        _statusCache[jobId] = status;

        _logger.LogInformation("导出任务状态更新: {JobId}, 状态: {Status}", jobId, status.Status);
    }

    public async Task<ExportJobStatus?> GetStatusAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        // 先从缓存获取
        if (_statusCache.TryGetValue(jobId, out var cachedStatus))
        {
            return cachedStatus;
        }

        // 从数据库获取
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var history = await dbContext.ReportExportHistories
            .AsNoTracking()
            .FirstOrDefaultAsync(h => h.Id == jobId, cancellationToken);

        if (history == null) return null;

        var status = new ExportJobStatus
        {
            JobId = history.Id,
            Status = history.Status,
            RecordCount = history.RecordCount,
            FileSize = history.FileSize,
            FilePath = history.FilePath,
            ErrorMessage = history.ErrorMessage,
            CreatedAt = history.CreatedAt,
            CompletedAt = history.CompletedAt
        };

        // 存入缓存
        _statusCache[jobId] = status;

        return status;
    }

    public async Task<int> GetPendingCountAsync(CancellationToken cancellationToken = default)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        return await dbContext.ReportExportHistories
            .AsNoTracking()
            .CountAsync(h => h.Status == "pending" || h.Status == "processing", cancellationToken);
    }

    public async Task CleanupAsync(TimeSpan olderThan, CancellationToken cancellationToken = default)
    {
        var cutoffDate = DateTime.UtcNow.Subtract(olderThan);

        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var oldJobs = await dbContext.ReportExportHistories
            .Where(h => (h.Status == "completed" || h.Status == "failed") && h.CreatedAt < cutoffDate)
            .ToListAsync(cancellationToken);

        foreach (var job in oldJobs)
        {
            // 删除文件
            if (!string.IsNullOrEmpty(job.FilePath) && File.Exists(job.FilePath))
            {
                try
                {
                    File.Delete(job.FilePath);
                    _logger.LogDebug("删除过期导出文件: {FilePath}", job.FilePath);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "删除导出文件失败: {FilePath}", job.FilePath);
                }
            }

            // 清理缓存
            _statusCache.TryRemove(job.Id, out _);
        }

        // 批量删除记录
        dbContext.ReportExportHistories.RemoveRange(oldJobs);
        await dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("清理完成，删除了 {Count} 条旧导出记录", oldJobs.Count);
    }
}

/// <summary>
/// 线程安全的优先级队列
/// </summary>
public class ConcurrentPriorityQueue<TElement, TPriority> where TPriority : IComparable<TPriority>
{
    private readonly SortedDictionary<TPriority, ConcurrentQueue<TElement>> _queues = new();
    private readonly object _lock = new();

    public void Enqueue(TElement element, TPriority priority)
    {
        lock (_lock)
        {
            if (!_queues.ContainsKey(priority))
            {
                _queues[priority] = new ConcurrentQueue<TElement>();
            }
            _queues[priority].Enqueue(element);
        }
    }

    public bool TryDequeue(out TElement element)
    {
        lock (_lock)
        {
            foreach (var kvp in _queues)
            {
                if (kvp.Value.TryDequeue(out element!))
                {
                    return true;
                }
            }
        }
        element = default!;
        return false;
    }

    public int Count
    {
        get
        {
            lock (_lock)
            {
                return _queues.Values.Sum(q => q.Count);
            }
        }
    }
}
