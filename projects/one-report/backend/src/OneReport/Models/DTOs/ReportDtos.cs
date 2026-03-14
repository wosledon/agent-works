namespace OneReport.Models.DTOs;

/// <summary>
/// 报表定义DTO
/// </summary>
public class ReportDefinitionDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string DataSourceType { get; set; } = string.Empty;
    public string? DataSourceConfig { get; set; }
    public string? QueryTemplate { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public List<ReportColumnDto> Columns { get; set; } = new();
}

/// <summary>
/// 创建报表定义请求
/// </summary>
public class CreateReportDefinitionDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string DataSourceType { get; set; } = string.Empty;
    public string? DataSourceConfig { get; set; }
    public string? QueryTemplate { get; set; }
    public List<CreateReportColumnDto> Columns { get; set; } = new();
}

/// <summary>
/// 更新报表定义请求
/// </summary>
public class UpdateReportDefinitionDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string DataSourceType { get; set; } = string.Empty;
    public string? DataSourceConfig { get; set; }
    public string? QueryTemplate { get; set; }
    public bool IsActive { get; set; }
    public List<UpdateReportColumnDto> Columns { get; set; } = new();
}

/// <summary>
/// 报表列DTO
/// </summary>
public class ReportColumnDto
{
    public Guid Id { get; set; }
    public string FieldName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public bool IsVisible { get; set; }
    public string? Format { get; set; }
    public int? Width { get; set; }
    public string? Aggregation { get; set; }
}

/// <summary>
/// 创建报表列请求
/// </summary>
public class CreateReportColumnDto
{
    public string FieldName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string DataType { get; set; } = "string";
    public int DisplayOrder { get; set; }
    public bool IsVisible { get; set; } = true;
    public string? Format { get; set; }
    public int? Width { get; set; }
    public string? Aggregation { get; set; }
}

/// <summary>
/// 更新报表列请求
/// </summary>
public class UpdateReportColumnDto
{
    public Guid? Id { get; set; } // 新列没有ID
    public string FieldName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string DataType { get; set; } = "string";
    public int DisplayOrder { get; set; }
    public bool IsVisible { get; set; } = true;
    public string? Format { get; set; }
    public int? Width { get; set; }
    public string? Aggregation { get; set; }
}

/// <summary>
/// 数据源DTO
/// </summary>
public class DataSourceDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string ConnectionString { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// 创建数据源请求
/// </summary>
public class CreateDataSourceDto
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string ConnectionString { get; set; } = string.Empty;
}

/// <summary>
/// 更新数据源请求
/// </summary>
public class UpdateDataSourceDto
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string ConnectionString { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

// ========================================
// M3: 新增 DTOs
// ========================================

/// <summary>
/// 报表执行日志DTO
/// </summary>
public class ReportExecutionLogDto
{
    public Guid Id { get; set; }
    public Guid? ReportDefinitionId { get; set; }
    public string? ReportName { get; set; }
    public string OperationType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public long? ExecutionTimeMs { get; set; }
    public long? RecordCount { get; set; }
    public Dictionary<string, object?>? Parameters { get; set; }
    public string? ErrorMessage { get; set; }
    public Guid? ExecutedBy { get; set; }
    public string? ExportFormat { get; set; }
    public bool UsedCache { get; set; }
}

/// <summary>
/// 报表执行统计DTO
/// </summary>
public class ReportExecutionStatsDto
{
    public long TotalExecutions { get; set; }
    public long SuccessfulExecutions { get; set; }
    public long FailedExecutions { get; set; }
    public double AverageExecutionTimeMs { get; set; }
    public long TotalRecordsProcessed { get; set; }
    public Dictionary<string, long> ExecutionsByOperationType { get; set; } = new();
    public Dictionary<string, long> ExecutionsByDay { get; set; } = new();
}

/// <summary>
/// 缓存统计DTO
/// </summary>
public class CacheStatsDto
{
    public long TotalCacheEntries { get; set; }
    public long TotalCacheSize { get; set; }
    public long ExpiredEntries { get; set; }
    public long TotalHits { get; set; }
    public double CacheHitRate { get; set; }
    public List<CacheEntryDto> TopHitEntries { get; set; } = new();
}

/// <summary>
/// 缓存条目DTO
/// </summary>
public class CacheEntryDto
{
    public Guid Id { get; set; }
    public Guid? ReportDefinitionId { get; set; }
    public string? ReportName { get; set; }
    public int RecordCount { get; set; }
    public long DataSize { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public int HitCount { get; set; }
    public DateTime? LastHitAt { get; set; }
}

/// <summary>
/// 用户权限DTO
/// </summary>
public class UserPermissionDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string ResourceType { get; set; } = string.Empty;
    public Guid? ResourceId { get; set; }
    public string Permission { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// 用户角色DTO
/// </summary>
public class UserRoleDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Role { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// 创建用户权限请求
/// </summary>
public class CreateUserPermissionDto
{
    public Guid UserId { get; set; }
    public string ResourceType { get; set; } = string.Empty;
    public Guid? ResourceId { get; set; }
    public string Permission { get; set; } = string.Empty;
}

/// <summary>
/// 设置用户角色请求
/// </summary>
public class SetUserRoleDto
{
    public Guid UserId { get; set; }
    public string Role { get; set; } = string.Empty;
}

/// <summary>
/// 分页数据源列表DTO
/// </summary>
public class DataSourceListDto
{
    public List<DataSourceDto> Items { get; set; } = new();
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public long TotalCount { get; set; }
    public int TotalPages { get; set; }
}