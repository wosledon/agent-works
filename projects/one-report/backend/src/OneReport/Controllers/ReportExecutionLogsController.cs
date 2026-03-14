using Microsoft.AspNetCore.Mvc;
using OneReport.Models.DTOs;
using OneReport.Models.Requests;
using OneReport.Models.Responses;
using OneReport.Services.Interfaces;

namespace OneReport.Controllers;

/// <summary>
/// 报表执行日志 API - 查询执行历史和性能指标
/// </summary>[ApiController]
[Route("api/[controller]")]
public class ReportExecutionLogsController : ControllerBase
{
    private readonly IReportExecutionLogService _logService;
    private readonly ILogger<ReportExecutionLogsController> _logger;

    public ReportExecutionLogsController(
        IReportExecutionLogService logService,
        ILogger<ReportExecutionLogsController> logger)
    {
        _logService = logService;
        _logger = logger;
    }

    /// <summary>
    /// 查询执行日志
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResponse<ReportExecutionLogDto>>>> GetLogs(
        [FromQuery] QueryExecutionLogsRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _logService.GetLogsAsync(request, cancellationToken);
        return Ok(ApiResponse<PagedResponse<ReportExecutionLogDto>>.Ok(result));
    }

    /// <summary>
    /// 获取执行统计
    /// </summary>
    [HttpGet("stats")]
    public async Task<ActionResult<ApiResponse<ReportExecutionStatsDto>>> GetStats(
        [FromQuery] GetExecutionStatsRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _logService.GetExecutionStatsAsync(request, cancellationToken);
        return Ok(ApiResponse<ReportExecutionStatsDto>.Ok(result));
    }

    /// <summary>
    /// 清理过期日志
    /// </summary>
    [HttpPost("cleanup")]
    public async Task<ActionResult<ApiResponse<long>>> CleanupLogs(
        [FromBody] CleanupExecutionLogsRequest request,
        CancellationToken cancellationToken)
    {
        var count = await _logService.CleanupOldLogsAsync(request.KeepDays, cancellationToken);
        return Ok(ApiResponse<long>.Ok(count, $"已清理 {count} 条过期日志"));
    }
}