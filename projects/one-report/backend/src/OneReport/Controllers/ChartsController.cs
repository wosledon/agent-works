using Microsoft.AspNetCore.Mvc;
using OneReport.Models.DTOs;
using OneReport.Models.Requests;
using OneReport.Models.Responses;
using OneReport.Services.Interfaces;

namespace OneReport.Controllers;

/// <summary>
/// 图表管理 API
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ChartsController : ControllerBase
{
    private readonly IChartService _chartService;
    private readonly ILogger<ChartsController> _logger;

    public ChartsController(
        IChartService chartService,
        ILogger<ChartsController> logger)
    {
        _chartService = chartService;
        _logger = logger;
    }

    /// <summary>
    /// 获取图表列表（分页）
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<ChartDefinitionListDto>>> GetList(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _chartService.GetListAsync(pageNumber, pageSize, search, cancellationToken);
        return Ok(ApiResponse<ChartDefinitionListDto>.Ok(result));
    }

    /// <summary>
    /// 获取所有活跃图表（下拉选择用）
    /// </summary>
    [HttpGet("all")]
    public async Task<ActionResult<ApiResponse<List<ChartDefinitionDto>>>> GetAll(CancellationToken cancellationToken)
    {
        var result = await _chartService.GetAllAsync(cancellationToken);
        return Ok(ApiResponse<List<ChartDefinitionDto>>.Ok(result));
    }

    /// <summary>
    /// 获取单个图表
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<ChartDefinitionDto>>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _chartService.GetByIdAsync(id, cancellationToken);
        if (result == null)
            return NotFound(ApiResponse<ChartDefinitionDto>.Fail($"图表 {id} 不存在"));
        
        return Ok(ApiResponse<ChartDefinitionDto>.Ok(result));
    }

    /// <summary>
    /// 创建图表
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<ChartDefinitionDto>>> Create(
        [FromBody] CreateChartDefinitionDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _chartService.CreateAsync(dto, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, ApiResponse<ChartDefinitionDto>.Ok(result, "图表创建成功"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<ChartDefinitionDto>.Fail(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建图表失败");
            return BadRequest(ApiResponse<ChartDefinitionDto>.Fail("创建图表失败", new List<string> { ex.Message }));
        }
    }

    /// <summary>
    /// 更新图表
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<ChartDefinitionDto>>> Update(
        Guid id,
        [FromBody] UpdateChartDefinitionDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _chartService.UpdateAsync(id, dto, cancellationToken);
            if (result == null)
                return NotFound(ApiResponse<ChartDefinitionDto>.Fail($"图表 {id} 不存在"));
            
            return Ok(ApiResponse<ChartDefinitionDto>.Ok(result, "图表更新成功"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<ChartDefinitionDto>.Fail(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新图表失败: {Id}", id);
            return BadRequest(ApiResponse<ChartDefinitionDto>.Fail("更新图表失败", new List<string> { ex.Message }));
        }
    }

    /// <summary>
    /// 删除图表
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await _chartService.DeleteAsync(id, cancellationToken);
        if (!result)
            return NotFound(ApiResponse<bool>.Fail($"图表 {id} 不存在"));
        
        return Ok(ApiResponse<bool>.Ok(true, "图表删除成功"));
    }

    /// <summary>
    /// 获取图表数据
    /// </summary>
    [HttpGet("{id:guid}/data")]
    public async Task<ActionResult<ApiResponse<ChartDataResponse>>> GetChartData(
        Guid id,
        [FromQuery] Dictionary<string, string>? queryParams,
        CancellationToken cancellationToken)
    {
        try
        {
            // 转换 query 参数
            var parameters = queryParams?.ToDictionary(
                kvp => kvp.Key,
                kvp => (object?)kvp.Value
            );

            var result = await _chartService.GetChartDataAsync(id, parameters, cancellationToken);
            return Ok(ApiResponse<ChartDataResponse>.Ok(result));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<ChartDataResponse>.Fail(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取图表数据失败: {Id}", id);
            return BadRequest(ApiResponse<ChartDataResponse>.Fail("获取图表数据失败", new List<string> { ex.Message }));
        }
    }

    /// <summary>
    /// 执行图表查询（POST 方式，支持复杂参数）
    /// </summary>
    [HttpPost("{id:guid}/data")]
    public async Task<ActionResult<ApiResponse<ChartDataResponse>>> ExecuteChartQuery(
        Guid id,
        [FromBody] Dictionary<string, object?>? parameters,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _chartService.GetChartDataAsync(id, parameters, cancellationToken);
            return Ok(ApiResponse<ChartDataResponse>.Ok(result));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<ChartDataResponse>.Fail(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取图表数据失败: {Id}", id);
            return BadRequest(ApiResponse<ChartDataResponse>.Fail("获取图表数据失败", new List<string> { ex.Message }));
        }
    }
}
