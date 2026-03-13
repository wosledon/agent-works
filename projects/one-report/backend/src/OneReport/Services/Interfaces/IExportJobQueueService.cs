namespace OneReport.Services.Interfaces;

/// <summary>
/// 导出任务队列服务接口
/// </summary>
public interface IExportJobQueueService
{
    /// <summary>
    /// 提交导出任务
    /// </summary>
    Task<Guid> EnqueueAsync(ExportJobRequest request, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 获取队列中的下一个任务
    /// </summary>
    Task<ExportJobRequest?> DequeueAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 更新任务状态
    /// </summary>
    Task UpdateStatusAsync(Guid jobId, ExportJobStatus status, string? message = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 获取任务状态
    /// </summary>
    Task<ExportJobStatus?> GetStatusAsync(Guid jobId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 获取待处理任务数量
    /// </summary>
    Task<int> GetPendingCountAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 清理已完成的任务
    /// </summary>
    Task CleanupAsync(TimeSpan olderThan, CancellationToken cancellationToken = default);
}

/// <summary>
/// 导出任务请求
/// </summary>
public class ExportJobRequest
{
    public Guid JobId { get; set; } = Guid.NewGuid();
    public Guid ReportDefinitionId { get; set; }
    public string Format { get; set; } = "csv";
    public Dictionary<string, object?>? Parameters { get; set; }
    public string? FileName { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ExportJobPriority Priority { get; set; } = ExportJobPriority.Normal;
}

/// <summary>
/// 导出任务状态
/// </summary>
public class ExportJobStatus
{
    public Guid JobId { get; set; }
    public string Status { get; set; } = "pending"; // pending, processing, completed, failed, cancelled
    public long? RecordCount { get; set; }
    public long? FileSize { get; set; }
    public string? FilePath { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int ProgressPercent { get; set; }
}

/// <summary>
/// 导出任务优先级
/// </summary>
public enum ExportJobPriority
{
    Low = 0,
    Normal = 1,
    High = 2,
    Critical = 3
}
