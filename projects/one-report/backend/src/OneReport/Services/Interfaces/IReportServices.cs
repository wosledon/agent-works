using OneReport.Models.DTOs;
using OneReport.Models.Requests;
using OneReport.Models.Responses;

namespace OneReport.Services.Interfaces;

/// <summary>
/// 报表定义服务接口
/// </summary>
public interface IReportDefinitionService
{
    Task<ReportDefinitionDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PagedResponse<ReportDefinitionDto>> GetListAsync(int pageNumber, int pageSize, string? search = null, CancellationToken cancellationToken = default);
    Task<ReportDefinitionDto> CreateAsync(CreateReportDefinitionDto dto, CancellationToken cancellationToken = default);
    Task<ReportDefinitionDto?> UpdateAsync(Guid id, UpdateReportDefinitionDto dto, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

/// <summary>
/// 报表导出服务接口 - 支持流式导出以优化内存使用
/// </summary>
public interface IReportExportService
{
    /// <summary>
    /// 导出报表为CSV格式 (流式)
    /// </summary>
    Task<Stream> ExportToCsvAsync(Guid reportDefinitionId, Dictionary<string, object>? parameters, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 导出报表为Excel格式 (流式)
    /// </summary>
    Task<Stream> ExportToExcelAsync(Guid reportDefinitionId, Dictionary<string, object>? parameters, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 导出报表为JSON格式 (流式)
    /// </summary>
    Task<Stream> ExportToJsonAsync(Guid reportDefinitionId, Dictionary<string, object>? parameters, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 创建异步导出任务
    /// </summary>
    Task<ExportJobResponse> CreateExportJobAsync(ExportReportRequest request, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 获取导出任务进度
    /// </summary>
    Task<ExportProgressResponse?> GetExportProgressAsync(Guid exportId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 获取导出文件
    /// </summary>
    Task<(string fileName, Stream fileStream)?> GetExportFileAsync(Guid exportId, CancellationToken cancellationToken = default);
}

/// <summary>
/// 报表数据服务接口
/// </summary>
public interface IReportDataService
{
    /// <summary>
    /// 预览报表数据 (分页)
    /// </summary>
    Task<ReportPreviewResponse> PreviewAsync(Guid reportDefinitionId, Dictionary<string, object>? parameters, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 流式读取报表数据 (用于大批量导出，低内存占用)
    /// </summary>
    IAsyncEnumerable<Dictionary<string, object>> StreamDataAsync(Guid reportDefinitionId, Dictionary<string, object>? parameters, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 获取报表总记录数
    /// </summary>
    Task<long> GetTotalCountAsync(Guid reportDefinitionId, Dictionary<string, object>? parameters, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 执行原始查询
    /// </summary>
    Task<QueryResultResponse> ExecuteQueryAsync(ExecuteQueryRequest request, CancellationToken cancellationToken = default);
}

/// <summary>
/// 数据源服务接口
/// </summary>
public interface IDataSourceService
{
    Task<DataSourceDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<DataSourceDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<DataSourceDto> CreateAsync(CreateDataSourceDto dto, CancellationToken cancellationToken = default);
    Task<DataSourceDto?> UpdateAsync(Guid id, UpdateDataSourceDto dto, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> TestConnectionAsync(TestConnectionRequest request, CancellationToken cancellationToken = default);
}
