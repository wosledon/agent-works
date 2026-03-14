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
    Task<Stream> ExportToCsvAsync(Guid reportDefinitionId, Dictionary<string, object?>? parameters, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 导出报表为Excel格式 (流式)
    /// </summary>
    Task<Stream> ExportToExcelAsync(Guid reportDefinitionId, Dictionary<string, object?>? parameters, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 导出报表为JSON格式 (流式)
    /// </summary>
    Task<Stream> ExportToJsonAsync(Guid reportDefinitionId, Dictionary<string, object?>? parameters, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 导出报表为PDF格式 (流式)
    /// </summary>
    Task<Stream> ExportToPdfAsync(Guid reportDefinitionId, Dictionary<string, object?>? parameters, CancellationToken cancellationToken = default);
    
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
    Task<ReportPreviewResponse> PreviewAsync(Guid reportDefinitionId, Dictionary<string, object?>? parameters, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 流式读取报表数据 (用于大批量导出，低内存占用)
    /// </summary>
    IAsyncEnumerable<Dictionary<string, object?>> StreamDataAsync(Guid reportDefinitionId, Dictionary<string, object?>? parameters, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 获取报表总记录数
    /// </summary>
    Task<long> GetTotalCountAsync(Guid reportDefinitionId, Dictionary<string, object?>? parameters, CancellationToken cancellationToken = default);
    
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
    Task<DataSourceListDto> GetListAsync(int pageNumber, int pageSize, string? search = null, CancellationToken cancellationToken = default);
    Task<List<DataSourceDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<DataSourceDto> CreateAsync(CreateDataSourceDto dto, CancellationToken cancellationToken = default);
    Task<DataSourceDto?> UpdateAsync(Guid id, UpdateDataSourceDto dto, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> TestConnectionAsync(TestConnectionRequest request, CancellationToken cancellationToken = default);
    Task<bool> TestConnectionByIdAsync(Guid id, CancellationToken cancellationToken = default);
}

/// <summary>
/// 查询结果缓存服务接口 - 用于数据库层面的查询结果缓存
/// </summary>
public interface IQueryResultCacheService
{
    /// <summary>
    /// 获取缓存的查询结果
    /// </summary>
    Task<ReportPreviewResponse?> GetCachedResultAsync(Guid reportDefinitionId, Dictionary<string, object?>? parameters, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 缓存查询结果
    /// </summary>
    Task CacheResultAsync(Guid reportDefinitionId, Dictionary<string, object?>? parameters, ReportPreviewResponse result, TimeSpan? expiration = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 清除报表的所有缓存
    /// </summary>
    Task InvalidateCacheAsync(Guid reportDefinitionId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 获取缓存统计信息
    /// </summary>
    Task<CacheStatsDto> GetCacheStatsAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 清理过期缓存
    /// </summary>
    Task<long> CleanupExpiredAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// 报表执行日志服务接口
/// </summary>
public interface IReportExecutionLogService
{
    /// <summary>
    /// 开始记录执行
    /// </summary>
    Task<Guid> BeginExecutionAsync(Guid? reportDefinitionId, string operationType, Dictionary<string, object?>? parameters = null, string? clientIp = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 完成执行记录
    /// </summary>
    Task CompleteExecutionAsync(Guid logId, long? recordCount = null, string? exportFormat = null, long? fileSize = null, bool usedCache = false, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 记录执行失败
    /// </summary>
    Task FailExecutionAsync(Guid logId, string errorMessage, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 查询执行日志
    /// </summary>
    Task<PagedResponse<ReportExecutionLogDto>> GetLogsAsync(QueryExecutionLogsRequest request, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 获取执行统计
    /// </summary>
    Task<ReportExecutionStatsDto> GetExecutionStatsAsync(GetExecutionStatsRequest request, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 清理旧日志
    /// </summary>
    Task<long> CleanupOldLogsAsync(int keepDays, CancellationToken cancellationToken = default);
}

/// <summary>
/// 权限服务接口 - 简单的用户权限管理
/// </summary>
public interface IPermissionService
{
    /// <summary>
    /// 检查用户是否有权限
    /// </summary>
    Task<bool> HasPermissionAsync(Guid userId, string resourceType, Guid? resourceId, string permission, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 检查用户是否是管理员
    /// </summary>
    Task<bool> IsAdminAsync(Guid userId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 获取用户权限列表
    /// </summary>
    Task<List<UserPermissionDto>> GetUserPermissionsAsync(Guid userId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 授予用户权限
    /// </summary>
    Task<UserPermissionDto> GrantPermissionAsync(CreateUserPermissionDto dto, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 撤销用户权限
    /// </summary>
    Task<bool> RevokePermissionAsync(Guid permissionId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 获取用户角色
    /// </summary>
    Task<UserRoleDto?> GetUserRoleAsync(Guid userId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 设置用户角色
    /// </summary>
    Task<UserRoleDto> SetUserRoleAsync(SetUserRoleDto dto, CancellationToken cancellationToken = default);
}