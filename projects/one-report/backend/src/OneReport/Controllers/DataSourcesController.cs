using Microsoft.AspNetCore.Mvc;
using OneReport.Models.DTOs;
using OneReport.Models.Requests;
using OneReport.Models.Responses;
using OneReport.Services.Interfaces;

namespace OneReport.Controllers;

/// <summary>
/// 数据源管理 API
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class DataSourcesController : ControllerBase
{
    private readonly IDataSourceService _dataSourceService;
    private readonly ILogger<DataSourcesController> _logger;

    public DataSourcesController(
        IDataSourceService dataSourceService,
        ILogger<DataSourcesController> logger)
    {
        _dataSourceService = dataSourceService;
        _logger = logger;
    }

    /// <summary>
    /// 获取数据源列表（分页）
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<DataSourceListDto>>> GetList(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _dataSourceService.GetListAsync(pageNumber, pageSize, search, cancellationToken);
        return Ok(ApiResponse<DataSourceListDto>.Ok(result));
    }

    /// <summary>
    /// 获取所有活跃数据源（下拉选择用）
    /// </summary>
    [HttpGet("all")]
    public async Task<ActionResult<ApiResponse<List<DataSourceDto>>>> GetAll(CancellationToken cancellationToken)
    {
        var result = await _dataSourceService.GetAllAsync(cancellationToken);
        return Ok(ApiResponse<List<DataSourceDto>>.Ok(result));
    }

    /// <summary>
    /// 获取单个数据源
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<DataSourceDto>>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _dataSourceService.GetByIdAsync(id, cancellationToken);
        if (result == null)
            return NotFound(ApiResponse<DataSourceDto>.Fail($"数据源 {id} 不存在"));
        
        return Ok(ApiResponse<DataSourceDto>.Ok(result));
    }

    /// <summary>
    /// 创建数据源
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<DataSourceDto>>> Create(
        [FromBody] CreateDataSourceDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _dataSourceService.CreateAsync(dto, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, ApiResponse<DataSourceDto>.Ok(result, "数据源创建成功"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<DataSourceDto>.Fail(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建数据源失败");
            return BadRequest(ApiResponse<DataSourceDto>.Fail("创建数据源失败", new List<string> { ex.Message }));
        }
    }

    /// <summary>
    /// 更新数据源
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<DataSourceDto>>> Update(
        Guid id,
        [FromBody] UpdateDataSourceDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _dataSourceService.UpdateAsync(id, dto, cancellationToken);
            if (result == null)
                return NotFound(ApiResponse<DataSourceDto>.Fail($"数据源 {id} 不存在"));
            
            return Ok(ApiResponse<DataSourceDto>.Ok(result, "数据源更新成功"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<DataSourceDto>.Fail(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新数据源失败: {Id}", id);
            return BadRequest(ApiResponse<DataSourceDto>.Fail("更新数据源失败", new List<string> { ex.Message }));
        }
    }

    /// <summary>
    /// 删除数据源
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await _dataSourceService.DeleteAsync(id, cancellationToken);
        if (!result)
            return NotFound(ApiResponse<bool>.Fail($"数据源 {id} 不存在"));
        
        return Ok(ApiResponse<bool>.Ok(true, "数据源删除成功"));
    }

    /// <summary>
    /// 测试数据源连接
    /// </summary>
    [HttpPost("test-connection")]
    public async Task<ActionResult<ApiResponse<bool>>> TestConnection(
        [FromBody] TestConnectionRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _dataSourceService.TestConnectionAsync(request, cancellationToken);
            if (result)
                return Ok(ApiResponse<bool>.Ok(true, "连接成功"));
            else
                return Ok(ApiResponse<bool>.Ok(false, "连接失败，请检查连接字符串"));
        }
        catch (NotSupportedException ex)
        {
            return BadRequest(ApiResponse<bool>.Fail(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "测试数据源连接失败");
            return BadRequest(ApiResponse<bool>.Fail("测试连接失败", new List<string> { ex.Message }));
        }
    }
}
