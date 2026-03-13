using Microsoft.AspNetCore.Mvc;
using OneReport.Models.DTOs;
using OneReport.Models.Requests;
using OneReport.Models.Responses;
using OneReport.Services.Interfaces;

namespace OneReport.Controllers;

/// <summary>
/// 报表定义管理 API
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ReportDefinitionsController : ControllerBase
{
    private readonly IReportDefinitionService _reportService;
    private readonly ILogger<ReportDefinitionsController> _logger;

    public ReportDefinitionsController(
        IReportDefinitionService reportService,
        ILogger<ReportDefinitionsController> logger)
    {
        _reportService = reportService;
        _logger = logger;
    }

    /// <summary>
    /// 获取报表定义列表
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResponse<ReportDefinitionDto>>>> GetList(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _reportService.GetListAsync(pageNumber, pageSize, search, cancellationToken);
        return Ok(ApiResponse<PagedResponse<ReportDefinitionDto>>.Ok(result));
    }

    /// <summary>
    /// 获取单个报表定义
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<ReportDefinitionDto>>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _reportService.GetByIdAsync(id, cancellationToken);
        if (result == null)
            return NotFound(ApiResponse<ReportDefinitionDto>.Fail($"报表定义 {id} 不存在"));
        
        return Ok(ApiResponse<ReportDefinitionDto>.Ok(result));
    }

    /// <summary>
    /// 创建报表定义
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<ReportDefinitionDto>>> Create(
        [FromBody] CreateReportDefinitionDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _reportService.CreateAsync(dto, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, ApiResponse<ReportDefinitionDto>.Ok(result, "报表定义创建成功"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建报表定义失败");
            return BadRequest(ApiResponse<ReportDefinitionDto>.Fail("创建报表定义失败", new List<string> { ex.Message }));
        }
    }

    /// <summary>
    /// 更新报表定义
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<ReportDefinitionDto>>> Update(
        Guid id,
        [FromBody] UpdateReportDefinitionDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _reportService.UpdateAsync(id, dto, cancellationToken);
            if (result == null)
                return NotFound(ApiResponse<ReportDefinitionDto>.Fail($"报表定义 {id} 不存在"));
            
            return Ok(ApiResponse<ReportDefinitionDto>.Ok(result, "报表定义更新成功"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新报表定义失败: {Id}", id);
            return BadRequest(ApiResponse<ReportDefinitionDto>.Fail("更新报表定义失败", new List<string> { ex.Message }));
        }
    }

    /// <summary>
    /// 删除报表定义
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await _reportService.DeleteAsync(id, cancellationToken);
        if (!result)
            return NotFound(ApiResponse<bool>.Fail($"报表定义 {id} 不存在"));
        
        return Ok(ApiResponse<bool>.Ok(true, "报表定义删除成功"));
    }
}
