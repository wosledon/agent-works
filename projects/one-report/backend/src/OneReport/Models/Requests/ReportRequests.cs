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
    public Dictionary<string, object>? Parameters { get; set; }
    
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
    public Dictionary<string, object>? Parameters { get; set; }
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
    public Dictionary<string, object>? Parameters { get; set; }
}
