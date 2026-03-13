using Microsoft.AspNetCore.Mvc;
using OneReport.Models.Requests;
using OneReport.Models.Responses;
using OneReport.Services.Interfaces;

namespace OneReport.Controllers;

/// <summary>
/// 报表数据查询 API
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ReportDataController : ControllerBase
{
    private readonly IReportDataService _dataService;
    private readonly ILogger<ReportDataController> _logger;

    public ReportDataController(
        IReportDataService dataService,
        ILogger<ReportDataController> logger)
    {
        _dataService = dataService;
        _logger = logger;
    }

    /// <summary>
    /// 预览报表数据 (分页)
    /// </summary>
    [HttpPost("preview")]
    public async Task<ActionResult<ApiResponse<ReportPreviewResponse>>> Preview(
        [FromBody] PreviewReportRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _dataService.PreviewAsync(
                request.ReportDefinitionId,
                request.Parameters,
                request.PageNumber,
                request.PageSize,
                cancellationToken);

            return Ok(ApiResponse<ReportPreviewResponse>.Ok(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "预览报表数据失败: {ReportId}", request.ReportDefinitionId);
            return BadRequest(ApiResponse<ReportPreviewResponse>.Fail("预览报表数据失败", new List<string> { ex.Message }));
        }
    }

    /// <summary>
    /// 获取报表总记录数
    /// </summary>
    [HttpPost("count")]
    public async Task<ActionResult<ApiResponse<long>>> GetCount(
        [FromBody] PreviewReportRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var count = await _dataService.GetTotalCountAsync(
                request.ReportDefinitionId,
                request.Parameters,
                cancellationToken);

            return Ok(ApiResponse<long>.Ok(count));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取报表记录数失败: {ReportId}", request.ReportDefinitionId);
            return BadRequest(ApiResponse<long>.Fail("获取报表记录数失败", new List<string> { ex.Message }));
        }
    }

    /// <summary>
    /// 执行原始查询
    /// </summary>
    [HttpPost("query")]
    public async Task<ActionResult<ApiResponse<QueryResultResponse>>> ExecuteQuery(
        [FromBody] ExecuteQueryRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _dataService.ExecuteQueryAsync(request, cancellationToken);
            return Ok(ApiResponse<QueryResultResponse>.Ok(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "执行查询失败");
            return BadRequest(ApiResponse<QueryResultResponse>.Fail("执行查询失败", new List<string> { ex.Message }));
        }
    }
}
