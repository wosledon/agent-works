using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OneReport.Data;
using OneReport.Data.Entities;
using OneReport.Models.DTOs;
using OneReport.Models.Requests;
using OneReport.Models.Responses;
using OneReport.Services.Interfaces;

namespace OneReport.Services.Implementations;

/// <summary>
/// 报表执行日志服务实现
/// </summary>
public class ReportExecutionLogService : IReportExecutionLogService
{
    private readonly AppDbContext _context;
    private readonly ILogger<ReportExecutionLogService> _logger;
    private readonly IHttpContextAccessor? _httpContextAccessor;

    public ReportExecutionLogService(
        AppDbContext context,
        ILogger<ReportExecutionLogService> logger,
        IHttpContextAccessor? httpContextAccessor = null)
    {
        _context = context;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<Guid> BeginExecutionAsync(
        Guid? reportDefinitionId, 
        string operationType, 
        Dictionary<string, object?>? parameters = null,
        string? clientIp = null,
        CancellationToken cancellationToken = default)
    {
        var log = new ReportExecutionLog
        {
            Id = Guid.NewGuid(),
            ReportDefinitionId = reportDefinitionId,
            OperationType = operationType.ToLower(),
            Status = "running",
            StartedAt = DateTime.UtcNow,
            Parameters = parameters != null ? JsonSerializer.Serialize(parameters) : null,
            ClientIp = clientIp ?? GetClientIp(),
            ExecutedBy = GetCurrentUserId()
        };

        _context.ReportExecutionLogs.Add(log);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "报表执行开始: {LogId}, 报表: {ReportId}, 操作: {OperationType}",
            log.Id, reportDefinitionId, operationType);

        return log.Id;
    }

    public async Task CompleteExecutionAsync(
        Guid logId, 
        long? recordCount = null,
        string? exportFormat = null,
        long? fileSize = null,
        bool usedCache = false,
        CancellationToken cancellationToken = default)
    {
        var log = await _context.ReportExecutionLogs.FindAsync(new object[] { logId }, cancellationToken);
        if (log == null)
        {
            _logger.LogWarning("未找到执行日志: {LogId}", logId);
            return;
        }

        log.Status = "completed";
        log.CompletedAt = DateTime.UtcNow;
        log.ExecutionTimeMs = (long)(log.CompletedAt.Value - log.StartedAt).TotalMilliseconds;
        log.RecordCount = recordCount;
        log.ExportFormat = exportFormat;
        log.FileSize = fileSize;
        log.UsedCache = usedCache;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "报表执行完成: {LogId}, 耗时: {ExecutionTimeMs}ms, 记录数: {RecordCount}, 使用缓存: {UsedCache}",
            logId, log.ExecutionTimeMs, recordCount, usedCache);
    }

    public async Task FailExecutionAsync(
        Guid logId, 
        string errorMessage,
        CancellationToken cancellationToken = default)
    {
        var log = await _context.ReportExecutionLogs.FindAsync(new object[] { logId }, cancellationToken);
        if (log == null)
        {
            _logger.LogWarning("未找到执行日志: {LogId}", logId);
            return;
        }

        log.Status = "failed";
        log.CompletedAt = DateTime.UtcNow;
        log.ExecutionTimeMs = (long)(log.CompletedAt.Value - log.StartedAt).TotalMilliseconds;
        log.ErrorMessage = errorMessage;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogError(
            "报表执行失败: {LogId}, 耗时: {ExecutionTimeMs}ms, 错误: {ErrorMessage}",
            logId, log.ExecutionTimeMs, errorMessage);
    }

    public async Task<PagedResponse<ReportExecutionLogDto>> GetLogsAsync(
        QueryExecutionLogsRequest request, 
        CancellationToken cancellationToken = default)
    {
        var query = _context.ReportExecutionLogs
            .AsNoTracking()
            .Include(l => l.ReportDefinition)
            .AsQueryable();

        // 应用筛选
        if (request.ReportDefinitionId.HasValue)
        {
            query = query.Where(l => l.ReportDefinitionId == request.ReportDefinitionId);
        }

        if (!string.IsNullOrEmpty(request.OperationType))
        {
            query = query.Where(l => l.OperationType == request.OperationType.ToLower());
        }

        if (!string.IsNullOrEmpty(request.Status))
        {
            query = query.Where(l => l.Status == request.Status.ToLower());
        }

        if (request.StartDate.HasValue)
        {
            query = query.Where(l => l.StartedAt >= request.StartDate.Value);
        }

        if (request.EndDate.HasValue)
        {
            query = query.Where(l => l.StartedAt <= request.EndDate.Value);
        }

        if (request.ExecutedBy.HasValue)
        {
            query = query.Where(l => l.ExecutedBy == request.ExecutedBy);
        }

        // 排序
        query = query.OrderByDescending(l => l.StartedAt);

        // 分页
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var dtos = items.Select(MapToDto).ToList();

        return new PagedResponse<ReportExecutionLogDto>
        {
            Items = dtos,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling((double)totalCount / request.PageSize),
            HasPreviousPage = request.PageNumber > 1,
            HasNextPage = request.PageNumber * request.PageSize < totalCount
        };
    }

    public async Task<ReportExecutionStatsDto> GetExecutionStatsAsync(
        GetExecutionStatsRequest request, 
        CancellationToken cancellationToken = default)
    {
        var query = _context.ReportExecutionLogs
            .AsNoTracking()
            .AsQueryable();

        // 应用时间筛选
        var startDate = request.StartDate ?? DateTime.UtcNow.AddDays(-30);
        var endDate = request.EndDate ?? DateTime.UtcNow;
        query = query.Where(l => l.StartedAt >= startDate && l.StartedAt <= endDate);

        if (request.ReportDefinitionId.HasValue)
        {
            query = query.Where(l => l.ReportDefinitionId == request.ReportDefinitionId);
        }

        var logs = await query.ToListAsync(cancellationToken);

        var stats = new ReportExecutionStatsDto
        {
            TotalExecutions = logs.Count,
            SuccessfulExecutions = logs.Count(l => l.Status == "completed"),
            FailedExecutions = logs.Count(l => l.Status == "failed"),
            TotalRecordsProcessed = logs.Where(l => l.RecordCount.HasValue).Sum(l => l.RecordCount!.Value),
            ExecutionsByOperationType = logs
                .GroupBy(l => l.OperationType)
                .ToDictionary(g => g.Key, g => (long)g.Count()),
            ExecutionsByDay = logs
                .GroupBy(l => l.StartedAt.ToString("yyyy-MM-dd"))
                .ToDictionary(g => g.Key, g => (long)g.Count())
        };

        // 计算平均执行时间
        var completedLogs = logs.Where(l => l.ExecutionTimeMs.HasValue).ToList();
        stats.AverageExecutionTimeMs = completedLogs.Any() 
            ? completedLogs.Average(l => l.ExecutionTimeMs!.Value) 
            : 0;

        return stats;
    }

    public async Task<long> CleanupOldLogsAsync(int keepDays, CancellationToken cancellationToken = default)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-keepDays);
        
        var logsToDelete = await _context.ReportExecutionLogs
            .Where(l => l.StartedAt < cutoffDate)
            .ToListAsync(cancellationToken);

        var count = logsToDelete.Count;
        
        if (count > 0)
        {
            _context.ReportExecutionLogs.RemoveRange(logsToDelete);
            await _context.SaveChangesAsync(cancellationToken);
            
            _logger.LogInformation("清理了 {Count} 条过期执行日志", count);
        }

        return count;
    }

    private ReportExecutionLogDto MapToDto(ReportExecutionLog log)
    {
        Dictionary<string, object?>? parameters = null;
        if (!string.IsNullOrEmpty(log.Parameters))
        {
            try
            {
                parameters = JsonSerializer.Deserialize<Dictionary<string, object?>>(log.Parameters);
            }
            catch { }
        }

        return new ReportExecutionLogDto
        {
            Id = log.Id,
            ReportDefinitionId = log.ReportDefinitionId,
            ReportName = log.ReportDefinition?.Name,
            OperationType = log.OperationType,
            Status = log.Status,
            StartedAt = log.StartedAt,
            CompletedAt = log.CompletedAt,
            ExecutionTimeMs = log.ExecutionTimeMs,
            RecordCount = log.RecordCount,
            Parameters = parameters,
            ErrorMessage = log.ErrorMessage,
            ExecutedBy = log.ExecutedBy,
            ExportFormat = log.ExportFormat,
            UsedCache = log.UsedCache
        };
    }

    private string? GetClientIp()
    {
        return _httpContextAccessor?.HttpContext?.Connection.RemoteIpAddress?.ToString();
    }

    private Guid? GetCurrentUserId()
    {
        // 这里可以根据实际情况从 JWT token 或 Session 中获取用户ID
        // 简化处理，返回 null
        return null;
    }
}