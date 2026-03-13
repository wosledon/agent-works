using System.Text.Json;

namespace OneReport.Models.Responses;

/// <summary>
/// API统一响应
/// </summary>
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public T? Data { get; set; }
    public List<string>? Errors { get; set; }

    public static ApiResponse<T> Ok(T data, string? message = null)
    {
        return new ApiResponse<T> { Success = true, Data = data, Message = message };
    }

    public static ApiResponse<T> Fail(string message, List<string>? errors = null)
    {
        return new ApiResponse<T> { Success = false, Message = message, Errors = errors };
    }
}

/// <summary>
/// 分页响应
/// </summary>
public class PagedResponse<T>
{
    public List<T> Items { get; set; } = new();
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public long TotalCount { get; set; }
    public int TotalPages { get; set; }
    public bool HasPreviousPage { get; set; }
    public bool HasNextPage { get; set; }
}

/// <summary>
/// 报表预览响应
/// </summary>
public class ReportPreviewResponse
{
    public List<Dictionary<string, object>> Data { get; set; } = new();
    public List<ColumnMeta> Columns { get; set; } = new();
    public long TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
}

/// <summary>
/// 列元数据
/// </summary>
public class ColumnMeta
{
    public string FieldName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
}

/// <summary>
/// 导出任务响应
/// </summary>
public class ExportJobResponse
{
    public Guid ExportId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? DownloadUrl { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// 导出进度响应
/// </summary>
public class ExportProgressResponse
{
    public Guid ExportId { get; set; }
    public string Status { get; set; } = string.Empty;
    public long? RecordCount { get; set; }
    public long? FileSize { get; set; }
    public double? ProgressPercentage { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime? CompletedAt { get; set; }
}

/// <summary>
/// 查询结果响应
/// </summary>
public class QueryResultResponse
{
    public List<Dictionary<string, object>> Data { get; set; } = new();
    public List<ColumnMeta> Columns { get; set; } = new();
    public long TotalCount { get; set; }
    public string? QueryExecutionTime { get; set; }
}
