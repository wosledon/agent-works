using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OneReport.Data;
using OneReport.Models.Responses;
using OneReport.Services.Interfaces;

namespace OneReport.Controllers;

/// <summary>
/// 系统 API - 健康检查、统计信息等
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class SystemController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IExportJobQueueService _jobQueueService;
    private readonly ILogger<SystemController> _logger;

    public SystemController(
        AppDbContext context,
        IExportJobQueueService jobQueueService,
        ILogger<SystemController> logger)
    {
        _context = context;
        _jobQueueService = jobQueueService;
        _logger = logger;
    }

    /// <summary>
    /// 健康检查
    /// </summary>
    [HttpGet("health")]
    public async Task<ActionResult<ApiResponse<object>>> Health(CancellationToken cancellationToken)
    {
        try
        {
            // 检查数据库连接
            await _context.Database.ExecuteSqlRawAsync("SELECT 1", cancellationToken);
            
            return Ok(ApiResponse<object>.Ok(new
            {
                Status = "healthy",
                Database = "connected",
                Timestamp = DateTime.UtcNow
            }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "健康检查失败");
            return StatusCode(503, ApiResponse<object>.Fail("服务不健康", new List<string> { ex.Message }));
        }
    }

    /// <summary>
    /// 获取系统统计信息
    /// </summary>
    [HttpGet("stats")]
    public async Task<ActionResult<ApiResponse<SystemStatsResponse>>> GetStats(CancellationToken cancellationToken)
    {
        try
        {
            var reportCount = await _context.ReportDefinitions.CountAsync(r => r.IsActive, cancellationToken);
            var dataSourceCount = await _context.DataSources.CountAsync(d => d.IsActive, cancellationToken);
            var exportHistoryCount = await _context.ReportExportHistories.CountAsync(cancellationToken);
            var pendingJobs = await _jobQueueService.GetPendingCountAsync(cancellationToken);

            var stats = new SystemStatsResponse
            {
                TotalReports = reportCount,
                TotalDataSources = dataSourceCount,
                TotalExports = exportHistoryCount,
                PendingExports = pendingJobs
            };

            return Ok(ApiResponse<SystemStatsResponse>.Ok(stats));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取统计信息失败");
            return BadRequest(ApiResponse<SystemStatsResponse>.Fail("获取统计信息失败", new List<string> { ex.Message }));
        }
    }

    /// <summary>
    /// 获取导出任务列表
    /// </summary>
    [HttpGet("exports")]
    public async Task<ActionResult<ApiResponse<List<ExportHistoryResponse>>>> GetExports(
        [FromQuery] string? status = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _context.ReportExportHistories
                .AsNoTracking()
                .OrderByDescending(e => e.CreatedAt)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(e => e.Status == status);
            }

            var totalCount = await query.CountAsync(cancellationToken);
            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(e => new ExportHistoryResponse
                {
                    Id = e.Id,
                    ReportDefinitionId = e.ReportDefinitionId,
                    ExportFormat = e.ExportFormat,
                    Status = e.Status,
                    RecordCount = e.RecordCount,
                    FileSize = e.FileSize,
                    CreatedAt = e.CreatedAt,
                    CompletedAt = e.CompletedAt,
                    ErrorMessage = e.ErrorMessage
                })
                .ToListAsync(cancellationToken);

            return Ok(ApiResponse<List<ExportHistoryResponse>>.Ok(items));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取导出历史失败");
            return BadRequest(ApiResponse<List<ExportHistoryResponse>>.Fail("获取导出历史失败", new List<string> { ex.Message }));
        }
    }
}

/// <summary>
/// 系统统计响应
/// </summary>
public class SystemStatsResponse
{
    public int TotalReports { get; set; }
    public int TotalDataSources { get; set; }
    public int TotalExports { get; set; }
    public int PendingExports { get; set; }
}

/// <summary>
/// 导出历史响应
/// </summary>
public class ExportHistoryResponse
{
    public Guid Id { get; set; }
    public Guid ReportDefinitionId { get; set; }
    public string ExportFormat { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public long? RecordCount { get; set; }
    public long? FileSize { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? ErrorMessage { get; set; }
}
