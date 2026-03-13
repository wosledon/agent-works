using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using OneReport.Models.Requests;
using OneReport.Models.Responses;
using OneReport.Services.Interfaces;

namespace OneReport.Controllers;

/// <summary>
/// 报表导出 API - 支持流式导出
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ReportExportsController : ControllerBase
{
    private readonly IReportExportService _exportService;
    private readonly ILogger<ReportExportsController> _logger;

    public ReportExportsController(
        IReportExportService exportService,
        ILogger<ReportExportsController> logger)
    {
        _exportService = exportService;
        _logger = logger;
    }

    /// <summary>
    /// 导出报表 (同步流式下载)
    /// </summary>
    [HttpPost("download")]
    public async Task<IActionResult> Download(
        [FromBody] ExportReportRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var format = request.Format.ToLower();
            var contentType = GetContentType(format);
            var fileExtension = GetFileExtension(format);
            var fileName = $"{request.FileName ?? "report"}_{DateTime.UtcNow:yyyyMMddHHmmss}{fileExtension}";

            Stream stream = format switch
            {
                "csv" => await _exportService.ExportToCsvAsync(request.ReportDefinitionId, request.Parameters, cancellationToken),
                "excel" or "xlsx" => await _exportService.ExportToExcelAsync(request.ReportDefinitionId, request.Parameters, cancellationToken),
                "json" => await _exportService.ExportToJsonAsync(request.ReportDefinitionId, request.Parameters, cancellationToken),
                _ => throw new NotSupportedException($"不支持的导出格式: {request.Format}")
            };

            _logger.LogInformation("报表导出下载: {ReportId}, 格式: {Format}", request.ReportDefinitionId, format);

            return File(stream, contentType, fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "导出报表失败: {ReportId}", request.ReportDefinitionId);
            return BadRequest(ApiResponse<object>.Fail("导出报表失败", new List<string> { ex.Message }));
        }
    }

    /// <summary>
    /// 创建异步导出任务
    /// </summary>
    [HttpPost("jobs")]
    public async Task<ActionResult<ApiResponse<ExportJobResponse>>> CreateExportJob(
        [FromBody] ExportReportRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _exportService.CreateExportJobAsync(request, cancellationToken);
            return AcceptedAtAction(
                nameof(GetExportProgress), 
                new { exportId = result.ExportId }, 
                ApiResponse<ExportJobResponse>.Ok(result, "导出任务已创建"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建导出任务失败: {ReportId}", request.ReportDefinitionId);
            return BadRequest(ApiResponse<ExportJobResponse>.Fail("创建导出任务失败", new List<string> { ex.Message }));
        }
    }

    /// <summary>
    /// 获取导出任务进度
    /// </summary>
    [HttpGet("jobs/{exportId:guid}")]
    public async Task<ActionResult<ApiResponse<ExportProgressResponse>>> GetExportProgress(
        Guid exportId,
        CancellationToken cancellationToken)
    {
        var result = await _exportService.GetExportProgressAsync(exportId, cancellationToken);
        
        if (result == null)
            return NotFound(ApiResponse<ExportProgressResponse>.Fail($"导出任务 {exportId} 不存在"));

        return Ok(ApiResponse<ExportProgressResponse>.Ok(result));
    }

    /// <summary>
    /// 下载导出的文件
    /// </summary>
    [HttpGet("jobs/{exportId:guid}/download")]
    public async Task<IActionResult> DownloadExportFile(Guid exportId, CancellationToken cancellationToken)
    {
        var result = await _exportService.GetExportFileAsync(exportId, cancellationToken);
        
        if (result == null)
            return NotFound(ApiResponse<object>.Fail($"导出文件不存在或尚未完成"));

        var (fileName, stream) = result.Value;
        var contentType = GetContentTypeByExtension(Path.GetExtension(fileName));

        return File(stream, contentType, fileName);
    }

    #region 私有方法

    private static string GetContentType(string format)
    {
        return format.ToLower() switch
        {
            "csv" => "text/csv",
            "excel" or "xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "json" => "application/json",
            _ => "application/octet-stream"
        };
    }

    private static string GetFileExtension(string format)
    {
        return format.ToLower() switch
        {
            "csv" => ".csv",
            "excel" or "xlsx" => ".xlsx",
            "json" => ".json",
            _ => ".txt"
        };
    }

    private static string GetContentTypeByExtension(string? extension)
    {
        var provider = new FileExtensionContentTypeProvider();
        if (!provider.TryGetContentType(extension ?? "", out var contentType))
        {
            contentType = "application/octet-stream";
        }
        return contentType;
    }

    #endregion
}
