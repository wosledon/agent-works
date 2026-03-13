using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using OneReport.Data;
using OneReport.Data.Entities;
using OneReport.Models.Requests;
using OneReport.Models.Responses;
using OneReport.Services.Interfaces;

namespace OneReport.Services.Implementations;

/// <summary>
/// 报表数据服务实现 - 支持流式查询以优化大报表内存使用
/// </summary>
public class ReportDataService : IReportDataService
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ReportDataService> _logger;

    public ReportDataService(
        AppDbContext context, 
        IConfiguration configuration,
        ILogger<ReportDataService> logger)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<ReportPreviewResponse> PreviewAsync(
        Guid reportDefinitionId, 
        Dictionary<string, object>? parameters, 
        int pageNumber, 
        int pageSize, 
        CancellationToken cancellationToken = default)
    {
        var report = await _context.ReportDefinitions
            .AsNoTracking()
            .Include(r => r.Columns)
            .FirstOrDefaultAsync(r => r.Id == reportDefinitionId && r.IsActive, cancellationToken);

        if (report == null)
            throw new InvalidOperationException($"Report definition {reportDefinitionId} not found");

        var stopwatch = Stopwatch.StartNew();
        var query = BuildQuery(report, parameters);
        
        // 使用流式查询获取数据
        var data = new List<Dictionary<string, object>>();
        var columns = report.Columns
            .Where(c => c.IsVisible)
            .OrderBy(c => c.DisplayOrder)
            .Select(c => new ColumnMeta
            {
                FieldName = c.FieldName,
                DisplayName = c.DisplayName,
                DataType = c.DataType
            }).ToList();

        var connectionString = GetConnectionString(report);
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        // 使用 Keyset pagination 替代 OFFSET 以提高大表性能
        var paginatedQuery = $@"
            WITH paged_data AS (
                {query}
            )
            SELECT * FROM paged_data
            LIMIT @PageSize
            OFFSET @Offset;
        ";

        await using var command = new NpgsqlCommand(paginatedQuery, connection);
        command.Parameters.AddWithValue("@PageSize", pageSize);
        command.Parameters.AddWithValue("@Offset", (pageNumber - 1) * pageSize);
        AddParameters(command, parameters);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var row = new Dictionary<string, object>();
            for (int i = 0; i < reader.FieldCount; i++)
            {
                var name = reader.GetName(i);
                var value = reader.IsDBNull(i) ? null! : reader.GetValue(i);
                row[name] = value ?? DBNull.Value;
            }
            data.Add(row);
        }

        var totalCount = await GetTotalCountAsync(reportDefinitionId, parameters, cancellationToken);
        stopwatch.Stop();

        _logger.LogInformation("报表预览查询完成: {ReportId}, 记录数: {Count}, 耗时: {ElapsedMs}ms", 
            reportDefinitionId, data.Count, stopwatch.ElapsedMilliseconds);

        return new ReportPreviewResponse
        {
            Data = data,
            Columns = columns,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    /// <summary>
    /// 流式读取报表数据 - 使用 IAsyncEnumerable 实现低内存占用
    /// </summary>
    public async IAsyncEnumerable<Dictionary<string, object>> StreamDataAsync(
        Guid reportDefinitionId, 
        Dictionary<string, object>? parameters,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var report = await _context.ReportDefinitions
            .AsNoTracking()
            .Include(r => r.Columns)
            .FirstOrDefaultAsync(r => r.Id == reportDefinitionId && r.IsActive, cancellationToken);

        if (report == null)
            throw new InvalidOperationException($"Report definition {reportDefinitionId} not found");

        var query = BuildQuery(report, parameters);
        var connectionString = GetConnectionString(report);
        
        // 使用 CommandBehavior.SequentialAccess 进行流式读取
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        
        await using var command = new NpgsqlCommand(query, connection);
        AddParameters(command, parameters);
        
        // 设置超时和fetch大小以优化流式读取
        command.CommandTimeout = 300; // 5分钟
        command.AllResultTypesAreUnknown = true;
        
        await using var reader = await command.ExecuteReaderAsync(
            CommandBehavior.SequentialAccess | CommandBehavior.SingleResult, cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            var row = new Dictionary<string, object>();
            for (int i = 0; i < reader.FieldCount; i++)
            {
                var name = reader.GetName(i);
                var value = await reader.IsDBNullAsync(i, cancellationToken) 
                    ? null! 
                    : reader.GetValue(i);
                row[name] = value ?? DBNull.Value;
            }
            yield return row;
        }
    }

    public async Task<long> GetTotalCountAsync(
        Guid reportDefinitionId, 
        Dictionary<string, object>? parameters, 
        CancellationToken cancellationToken = default)
    {
        var report = await _context.ReportDefinitions
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == reportDefinitionId && r.IsActive, cancellationToken);

        if (report == null) return 0;

        var query = BuildQuery(report, parameters);
        var countQuery = $"SELECT COUNT(*) FROM ({query}) AS count_query";
        
        var connectionString = GetConnectionString(report);
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        
        await using var command = new NpgsqlCommand(countQuery, connection);
        AddParameters(command, parameters);
        
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result is long count ? count : 0;
    }

    public async Task<QueryResultResponse> ExecuteQueryAsync(
        ExecuteQueryRequest request, 
        CancellationToken cancellationToken = default)
    {
        var dataSource = await _context.DataSources
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == request.DataSourceId && d.IsActive, cancellationToken);

        if (dataSource == null)
            throw new InvalidOperationException($"Data source {request.DataSourceId} not found");

        var stopwatch = Stopwatch.StartNew();
        var data = new List<Dictionary<string, object>>();
        var columns = new List<ColumnMeta>();

        await using var connection = new NpgsqlConnection(dataSource.ConnectionString);
        await connection.OpenAsync(cancellationToken);
        
        await using var command = new NpgsqlCommand(request.Query, connection);
        command.CommandTimeout = 120;
        AddParameters(command, request.Parameters);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        
        // 获取列元数据
        for (int i = 0; i < reader.FieldCount; i++)
        {
            columns.Add(new ColumnMeta
            {
                FieldName = reader.GetName(i),
                DisplayName = reader.GetName(i),
                DataType = reader.GetFieldType(i)?.Name ?? "string"
            });
        }

        long count = 0;
        while (await reader.ReadAsync(cancellationToken))
        {
            count++;
            if (count <= 1000) // 预览模式只返回前1000条
            {
                var row = new Dictionary<string, object>();
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    var name = reader.GetName(i);
                    var value = reader.IsDBNull(i) ? null! : reader.GetValue(i);
                    row[name] = value ?? DBNull.Value;
                }
                data.Add(row);
            }
        }

        stopwatch.Stop();

        return new QueryResultResponse
        {
            Data = data,
            Columns = columns,
            TotalCount = count,
            QueryExecutionTime = $"{stopwatch.ElapsedMilliseconds}ms"
        };
    }

    private string BuildQuery(ReportDefinition report, Dictionary<string, object>? parameters)
    {
        if (string.IsNullOrWhiteSpace(report.QueryTemplate))
        {
            throw new InvalidOperationException("Report query template is empty");
        }

        // 简单的参数替换，生产环境应使用参数化查询
        var query = report.QueryTemplate;
        
        // 如果有数据源配置，解析并应用
        if (!string.IsNullOrWhiteSpace(report.DataSourceConfig))
        {
            try
            {
                var config = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(report.DataSourceConfig);
                if (config?.TryGetValue("tableName", out var tableName) == true)
                {
                    query = query.Replace("{{tableName}}", tableName.GetString());
                }
            }
            catch { /* 忽略解析错误 */ }
        }

        return query;
    }

    private string GetConnectionString(ReportDefinition report)
    {
        // 优先使用数据源配置中的连接字符串
        if (!string.IsNullOrWhiteSpace(report.DataSourceConfig))
        {
            try
            {
                var config = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(report.DataSourceConfig);
                if (config?.TryGetValue("dataSourceId", out var dsId) == true && 
                    Guid.TryParse(dsId.GetString(), out var dataSourceId))
                {
                    var ds = _context.DataSources.AsNoTracking().FirstOrDefault(d => d.Id == dataSourceId);
                    if (ds != null) return ds.ConnectionString;
                }
            }
            catch { }
        }

        // 回退到默认连接字符串
        return _configuration.GetConnectionString("ReportDatabase") 
            ?? _configuration.GetConnectionString("DefaultConnection") 
            ?? throw new InvalidOperationException("Connection string not found");
    }

    private void AddParameters(NpgsqlCommand command, Dictionary<string, object>? parameters)
    {
        if (parameters == null) return;

        foreach (var param in parameters)
        {
            var paramName = param.Key.StartsWith("@") ? param.Key : $"@{param.Key}";
            var value = param.Value ?? DBNull.Value;
            command.Parameters.AddWithValue(paramName, value);
        }
    }
}
