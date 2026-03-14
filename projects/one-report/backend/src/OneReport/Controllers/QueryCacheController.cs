using Microsoft.AspNetCore.Mvc;
using OneReport.Models.DTOs;
using OneReport.Models.Requests;
using OneReport.Models.Responses;
using OneReport.Services.Interfaces;

namespace OneReport.Controllers;

/// <summary>
/// 查询缓存管理 API - 管理查询结果缓存
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class QueryCacheController : ControllerBase
{
    private readonly IQueryResultCacheService _cacheService;
    private readonly ILogger<QueryCacheController> _logger;

    public QueryCacheController(
        IQueryResultCacheService cacheService,
        ILogger<QueryCacheController> logger)
    {
        _cacheService = cacheService;
        _logger = logger;
    }

    /// <summary>
    /// 获取缓存统计信息
    /// </summary>
    [HttpGet("stats")]
    public async Task<ActionResult<ApiResponse<CacheStatsDto>>> GetStats(CancellationToken cancellationToken)
    {
        var result = await _cacheService.GetCacheStatsAsync(cancellationToken);
        return Ok(ApiResponse<CacheStatsDto>.Ok(result));
    }

    /// <summary>
    /// 清理过期缓存
    /// </summary>
    [HttpPost("cleanup")]
    public async Task<ActionResult<ApiResponse<long>>> CleanupCache(
        [FromBody] CleanupCacheRequest request,
        CancellationToken cancellationToken)
    {
        var count = await _cacheService.CleanupExpiredAsync(cancellationToken);
        return Ok(ApiResponse<long>.Ok(count, $"已清理 {count} 条过期缓存"));
    }

    /// <summary>
    /// 清除指定报表的缓存
    /// </summary>
    [HttpDelete("report/{reportId:guid}")]
    public async Task<ActionResult<ApiResponse<bool>>> InvalidateCache(Guid reportId, CancellationToken cancellationToken)
    {
        await _cacheService.InvalidateCacheAsync(reportId, cancellationToken);
        return Ok(ApiResponse<bool>.Ok(true, "缓存已清除"));
    }
}