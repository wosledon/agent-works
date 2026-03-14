namespace OneReport.Models.Requests;

/// <summary>
/// 导出报表请求
/// </summary>
public class ExportReportRequest
{
    /// <summary>
    /// 报表定义ID
    /// </summary>
    public Guid ReportDefinitionId { get; set; }
    
    /// <summary>
    /// 导出格式: csv, excel, json
    /// </summary>
    public string Format { get; set; } = "csv";
    
    /// <summary>
    /// 查询参数 (JSON格式)
    /// </summary>
    public Dictionary<string, object?>? Parameters { get; set; }
    
    /// <summary>
    /// 分页大小 (0表示不分页，流式导出)
    /// </summary>
    public int PageSize { get; set; } = 0;
    
    /// <summary>
    /// 文件名 (不含扩展名)
    /// </summary>
    public string? FileName { get; set; }
}

/// <summary>
/// 预览报表请求
/// </summary>
public class PreviewReportRequest
{
    public Guid ReportDefinitionId { get; set; }
    public Dictionary<string, object?>? Parameters { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 100;
}

/// <summary>
/// 测试数据源连接请求
/// </summary>
public class TestConnectionRequest
{
    public string Type { get; set; } = string.Empty;
    public string ConnectionString { get; set; } = string.Empty;
}

/// <summary>
/// 执行查询请求
/// </summary>
public class ExecuteQueryRequest
{
    public Guid DataSourceId { get; set; }
    public string Query { get; set; } = string.Empty;
    public Dictionary<string, object?>? Parameters { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 100;
    public int TimeoutSeconds { get; set; } = 30;
}

// ========================================
// M3: 新增请求模型
// ========================================

/// <summary>
/// 查询执行日志请求
/// </summary>
public class QueryExecutionLogsRequest
{
    public Guid? ReportDefinitionId { get; set; }
    public string? OperationType { get; set; }
    public string? Status { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public Guid? ExecutedBy { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

/// <summary>
/// 清理执行日志请求
/// </summary>
public class CleanupExecutionLogsRequest
{
    /// <summary>
    /// 保留最近多少天的日志
    /// </summary>
    public int KeepDays { get; set; } = 30;
}

/// <summary>
/// 清理过期缓存请求
/// </summary>
public class CleanupCacheRequest
{
    /// <summary>
    /// 强制清理所有过期缓存
    /// </summary>
    public bool ForceExpiredOnly { get; set; } = true;
}

/// <summary>
/// 获取执行统计请求
/// </summary>
public class GetExecutionStatsRequest
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public Guid? ReportDefinitionId { get; set; }
}