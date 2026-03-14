namespace OneReport.Data.Entities;

/// <summary>
/// 报表定义实体
/// </summary>
public class ReportDefinition
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string DataSourceType { get; set; } = string.Empty; // sql, api, file
    public string? DataSourceConfig { get; set; } // JSON配置
    public string? QueryTemplate { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public Guid CreatedBy { get; set; }
    
    // 导航属性
    public ICollection<ReportColumn> Columns { get; set; } = new List<ReportColumn>();
    public ICollection<ReportExportHistory> ExportHistories { get; set; } = new List<ReportExportHistory>();
}

/// <summary>
/// 报表列定义
/// </summary>
public class ReportColumn
{
    public Guid Id { get; set; }
    public Guid ReportDefinitionId { get; set; }
    public string FieldName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string DataType { get; set; } = "string"; // string, number, date, boolean
    public int DisplayOrder { get; set; }
    public bool IsVisible { get; set; } = true;
    public string? Format { get; set; } // 数字/日期格式
    public int? Width { get; set; }
    public string? Aggregation { get; set; } // sum, avg, count, etc.
    
    public ReportDefinition ReportDefinition { get; set; } = null!;
}

/// <summary>
/// 报表导出历史
/// </summary>
public class ReportExportHistory
{
    public Guid Id { get; set; }
    public Guid ReportDefinitionId { get; set; }
    public string ExportFormat { get; set; } = string.Empty; // csv, excel, json
    public string Status { get; set; } = "pending"; // pending, processing, completed, failed
    public long? RecordCount { get; set; }
    public long? FileSize { get; set; }
    public string? FilePath { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public Guid CreatedBy { get; set; }
    public string? Parameters { get; set; } // JSON格式的查询参数
    
    public ReportDefinition ReportDefinition { get; set; } = null!;
}

/// <summary>
/// 数据源配置
/// </summary>
public class DataSource
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // postgresql, mysql, api
    public string ConnectionString { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// 报表执行日志 - 记录执行历史和性能指标
/// </summary>
public class ReportExecutionLog
{
    public Guid Id { get; set; }
    public Guid? ReportDefinitionId { get; set; }
    public string OperationType { get; set; } = string.Empty; // preview, export, query
    public string Status { get; set; } = "running"; // running, completed, failed
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public long? ExecutionTimeMs { get; set; }
    public long? RecordCount { get; set; }
    public string? Parameters { get; set; } // JSON格式
    public string? ErrorMessage { get; set; }
    public Guid? ExecutedBy { get; set; }
    public string? ClientIp { get; set; }
    public string? ExportFormat { get; set; } // csv, excel, json, pdf
    public long? FileSize { get; set; }
    public bool UsedCache { get; set; } = false;
    
    // 导航属性
    public ReportDefinition? ReportDefinition { get; set; }
}

/// <summary>
/// 查询结果缓存 - 存储查询结果的缓存
/// </summary>
public class QueryResultCache
{
    public Guid Id { get; set; }
    public string CacheKey { get; set; } = string.Empty;
    public Guid? ReportDefinitionId { get; set; }
    public string? Parameters { get; set; } // JSON格式，用于缓存匹配
    public string Data { get; set; } = string.Empty; // JSON序列化的数据
    public long DataSize { get; set; }
    public int RecordCount { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
    public int HitCount { get; set; } = 0;
    public DateTime? LastHitAt { get; set; }
    
    // 导航属性
    public ReportDefinition? ReportDefinition { get; set; }
}

/// <summary>
/// 用户权限 - 简单的用户权限管理
/// </summary>
public class UserPermission
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string ResourceType { get; set; } = string.Empty; // report, datasource, system
    public Guid? ResourceId { get; set; } // null表示所有资源
    public string Permission { get; set; } = string.Empty; // view, edit, delete, execute
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? CreatedBy { get; set; }
}

/// <summary>
/// 用户角色 - 简化权限管理
/// </summary>
public class UserRole
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Role { get; set; } = string.Empty; // admin, editor, viewer
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// 图表定义
/// </summary>
public class ChartDefinition
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string ChartType { get; set; } = string.Empty; // line, bar, pie, scatter, etc.
    public Guid DataSourceId { get; set; }
    public string Query { get; set; } = string.Empty; // SQL 查询
    public string? Config { get; set; } // JSON 配置：xAxis, yAxis, series 等映射
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public Guid CreatedBy { get; set; }
    
    // 导航属性
    public DataSource DataSource { get; set; } = null!;
}
