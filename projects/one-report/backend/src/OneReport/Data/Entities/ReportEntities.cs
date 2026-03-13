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
