using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OneReport.Data;
using OneReport.Data.Entities;
using OneReport.Models.DTOs;
using OneReport.Models.Responses;
using OneReport.Services.Interfaces;

namespace OneReport.Services.Implementations;

/// <summary>
/// 查询结果缓存服务实现 - 数据库层面的查询结果缓存
/// </summary>
public class QueryResultCacheService : IQueryResultCacheService
{
    private readonly AppDbContext _context;
    private readonly ILogger<QueryResultCacheService> _logger;

    public QueryResultCacheService(
        AppDbContext context,
        ILogger<QueryResultCacheService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ReportPreviewResponse?> GetCachedResultAsync(
        Guid reportDefinitionId, 
        Dictionary<string, object?>? parameters,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = GenerateCacheKey(reportDefinitionId, parameters);
        
        var cacheEntry = await _context.QueryResultCaches
            .AsNoTracking()
            .Include(c => c.ReportDefinition)
            .FirstOrDefaultAsync(c => 
                c.CacheKey == cacheKey && 
                c.ExpiresAt > DateTime.UtcNow, 
                cancellationToken);

        if (cacheEntry == null)
        {
            _logger.LogDebug("缓存未命中: {CacheKey}", cacheKey);
            return null;
        }

        // 更新命中统计
        try
        {
            await _context.Database.ExecuteSqlRawAsync(
                "UPDATE query_result_caches SET hit_count = hit_count + 1, last_hit_at = {0} WHERE id = {1}",
                DateTime.UtcNow, cacheEntry.Id);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "更新缓存命中统计失败");
        }

        _logger.LogInformation(
            "缓存命中: {CacheKey}, 报表: {ReportId}, 命中次数: {HitCount}",
            cacheKey, reportDefinitionId, cacheEntry.HitCount + 1);

        // 反序列化数据
        try
        {
            var data = JsonSerializer.Deserialize<List<Dictionary<string, object?>>>(cacheEntry.Data);
            var columns = cacheEntry.ReportDefinition?.Columns
                .Where(c => c.IsVisible)
                .OrderBy(c => c.DisplayOrder)
                .Select(c => new ColumnMeta
                {
                    FieldName = c.FieldName,
                    DisplayName = c.DisplayName,
                    DataType = c.DataType
                }).ToList() ?? new List<ColumnMeta>();

            return new ReportPreviewResponse
            {
                Data = data ?? new List<Dictionary<string, object?>>(),
                Columns = columns,
                TotalCount = cacheEntry.RecordCount,
                PageNumber = 1,
                PageSize = cacheEntry.RecordCount
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "反序列化缓存数据失败: {CacheKey}", cacheKey);
            return null;
        }
    }

    public async Task CacheResultAsync(
        Guid reportDefinitionId, 
        Dictionary<string, object?>? parameters, 
        ReportPreviewResponse result,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = GenerateCacheKey(reportDefinitionId, parameters);
        var expirationTime = expiration ?? TimeSpan.FromMinutes(30);
        
        // 序列化数据
        var jsonData = JsonSerializer.Serialize(result.Data);
        var dataSize = Encoding.UTF8.GetByteCount(jsonData);

        // 检查是否已存在
        var existingCache = await _context.QueryResultCaches
            .FirstOrDefaultAsync(c => c.CacheKey == cacheKey, cancellationToken);

        if (existingCache != null)
        {
            // 更新现有缓存
            existingCache.Data = jsonData;
            existingCache.DataSize = dataSize;
            existingCache.RecordCount = (int)result.TotalCount;
            existingCache.ExpiresAt = DateTime.UtcNow.Add(expirationTime);
            existingCache.HitCount = 0;
            existingCache.LastHitAt = null;
        }
        else
        {
            // 创建新缓存
            var cacheEntry = new QueryResultCache
            {
                Id = Guid.NewGuid(),
                CacheKey = cacheKey,
                ReportDefinitionId = reportDefinitionId,
                Parameters = parameters != null ? JsonSerializer.Serialize(parameters) : null,
                Data = jsonData,
                DataSize = dataSize,
                RecordCount = (int)result.TotalCount,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.Add(expirationTime)
            };

            _context.QueryResultCaches.Add(cacheEntry);
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "查询结果已缓存: {CacheKey}, 报表: {ReportId}, 记录数: {RecordCount}, 数据大小: {DataSize} bytes, 过期时间: {ExpiresAt}",
            cacheKey, reportDefinitionId, result.TotalCount, dataSize, DateTime.UtcNow.Add(expirationTime));
    }

    public async Task InvalidateCacheAsync(Guid reportDefinitionId, CancellationToken cancellationToken = default)
    {
        var cachesToDelete = await _context.QueryResultCaches
            .Where(c => c.ReportDefinitionId == reportDefinitionId)
            .ToListAsync(cancellationToken);

        if (cachesToDelete.Any())
        {
            _context.QueryResultCaches.RemoveRange(cachesToDelete);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "已清除报表缓存: {ReportId}, 清除条目数: {Count}",
                reportDefinitionId, cachesToDelete.Count);
        }
    }

    public async Task<CacheStatsDto> GetCacheStatsAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        
        var totalEntries = await _context.QueryResultCaches.CountAsync(cancellationToken);
        var expiredEntries = await _context.QueryResultCaches
            .CountAsync(c => c.ExpiresAt <= now, cancellationToken);
        var totalSize = await _context.QueryResultCaches
            .SumAsync(c => c.DataSize, cancellationToken);
        var totalHits = await _context.QueryResultCaches
            .SumAsync(c => (long)c.HitCount, cancellationToken);

        // 获取热门缓存条目
        var topEntries = await _context.QueryResultCaches
            .AsNoTracking()
            .Include(c => c.ReportDefinition)
            .Where(c => c.HitCount > 0)
            .OrderByDescending(c => c.HitCount)
            .Take(10)
            .Select(c => new CacheEntryDto
            {
                Id = c.Id,
                ReportDefinitionId = c.ReportDefinitionId,
                ReportName = c.ReportDefinition != null ? c.ReportDefinition.Name : null,
                RecordCount = c.RecordCount,
                DataSize = c.DataSize,
                CreatedAt = c.CreatedAt,
                ExpiresAt = c.ExpiresAt,
                HitCount = c.HitCount,
                LastHitAt = c.LastHitAt
            })
            .ToListAsync(cancellationToken);

        // 计算缓存命中率（简化计算：总命中数 / (总条目数 + 总命中数)）
        var totalAccesses = totalEntries + totalHits;
        var hitRate = totalAccesses > 0 ? (double)totalHits / totalAccesses : 0;

        return new CacheStatsDto
        {
            TotalCacheEntries = totalEntries,
            TotalCacheSize = totalSize,
            ExpiredEntries = expiredEntries,
            TotalHits = totalHits,
            CacheHitRate = Math.Round(hitRate, 4),
            TopHitEntries = topEntries
        };
    }

    public async Task<long> CleanupExpiredAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        
        var expiredCaches = await _context.QueryResultCaches
            .Where(c => c.ExpiresAt <= now)
            .ToListAsync(cancellationToken);

        var count = expiredCaches.Count;
        
        if (count > 0)
        {
            _context.QueryResultCaches.RemoveRange(expiredCaches);
            await _context.SaveChangesAsync(cancellationToken);
            
            _logger.LogInformation("清理了 {Count} 条过期缓存", count);
        }

        return count;
    }

    private string GenerateCacheKey(Guid reportDefinitionId, Dictionary<string, object?>? parameters)
    {
        var keyBuilder = new StringBuilder();
        keyBuilder.Append($"report:{reportDefinitionId}");

        if (parameters != null && parameters.Any())
        {
            // 对参数进行排序以确保一致的键
            var sortedParams = parameters.OrderBy(p => p.Key);
            foreach (var param in sortedParams)
            {
                keyBuilder.Append($"|{param.Key}={param.Value}");
            }
        }

        // 使用 MD5 生成固定长度的键
        using var md5 = MD5.Create();
        var inputBytes = Encoding.UTF8.GetBytes(keyBuilder.ToString());
        var hashBytes = md5.ComputeHash(inputBytes);
        
        var hash = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
        return $"cache:{hash.Substring(0, 16)}";
    }
}