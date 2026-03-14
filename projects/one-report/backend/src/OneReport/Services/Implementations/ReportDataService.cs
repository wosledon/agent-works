using System.Data;
using System.Diagnostics;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using Npgsql;
using OneReport.Data;
using OneReport.Data.Entities;
using OneReport.Models.Requests;
using OneReport.Models.Responses;
using OneReport.Services.Interfaces;

namespace OneReport.Services.Implementations;

/// <summary>
/// 报表数据服务实现 - 支持流式查询以优化大报表内存使用
/// 支持 PostgreSQL、MySQL、API 数据源
/// </summary>
public class ReportDataService : IReportDataService
{
    private readonly AppDbContext _context;
    private readonly IApiDataSourceService _apiDataSourceService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ReportDataService> _logger;

    public ReportDataService(
        AppDbContext context, 
        IApiDataSourceService apiDataSourceService,
        IConfiguration configuration,
        ILogger<ReportDataService> logger)
    {
        _context = context;
        _apiDataSourceService = apiDataSourceService;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<ReportPreviewResponse> PreviewAsync(
        Guid reportDefinitionId, 
        Dictionary<string, object?>? parameters, 
        int pageNumber, 
        int pageSize, 
        CancellationToken cancellationToken = default)
    {
        var report = await _context.ReportDefinitions
            .AsNoTracking()
            .Include(r => r.Columns)
            .FirstOrDefaultAsync(r => r.Id == reportDefinitionId && r.IsActive, cancellationToken);

        if (report == null)
            throw new InvalidOperationException($"报表定义 {reportDefinitionId} 不存在");

        var stopwatch = Stopwatch.StartNew();
        
        var columns = report.Columns
            .Where(c => c.IsVisible)
            .OrderBy(c => c.DisplayOrder)
            .Select(c => new ColumnMeta
            {
                FieldName = c.FieldName,
                DisplayName = c.DisplayName,
                DataType = c.DataType
            }).ToList();

        List<Dictionary<string, object?>> data;
        
        // 根据数据源类型选择查询方式
        if (IsApiDataSource(report))
        {
            data = await QueryApiPreviewAsync(report, parameters, pageNumber, pageSize, cancellationToken);
        }
        else
        {
            data = await QueryDatabasePreviewAsync(report, parameters, pageNumber, pageSize, cancellationToken);
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
    public async IAsyncEnumerable<Dictionary<string, object?>> StreamDataAsync(
        Guid reportDefinitionId, 
        Dictionary<string, object?>? parameters,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var report = await _context.ReportDefinitions
            .AsNoTracking()
            .Include(r => r.Columns)
            .FirstOrDefaultAsync(r => r.Id == reportDefinitionId && r.IsActive, cancellationToken);

        if (report == null)
            throw new InvalidOperationException($"报表定义 {reportDefinitionId} 不存在");

        if (IsApiDataSource(report))
        {
            await foreach (var row in StreamApiDataAsync(report, parameters, cancellationToken))
            {
                yield return row;
            }
        }
        else
        {
            await foreach (var row in StreamDatabaseDataAsync(report, parameters, cancellationToken))
            {
                yield return row;
            }
        }
    }

    public async Task<long> GetTotalCountAsync(
        Guid reportDefinitionId, 
        Dictionary<string, object?>? parameters, 
        CancellationToken cancellationToken = default)
    {
        var report = await _context.ReportDefinitions
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == reportDefinitionId && r.IsActive, cancellationToken);

        if (report == null) return 0;

        // API 数据源无法获取精确总数，返回 -1 表示未知
        if (IsApiDataSource(report))
            return -1;

        var query = BuildQuery(report, parameters);
        var countQuery = $"SELECT COUNT(*) FROM ({query}) AS count_query";
        
        var connectionString = GetConnectionString(report);
        var dataSourceType = GetDataSourceType(report);
        
        return await ExecuteScalarAsync(connectionString, dataSourceType, countQuery, parameters, cancellationToken);
    }

    public async Task<QueryResultResponse> ExecuteQueryAsync(
        ExecuteQueryRequest request, 
        CancellationToken cancellationToken = default)
    {
        var dataSource = await _context.DataSources
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == request.DataSourceId && d.IsActive, cancellationToken);

        if (dataSource == null)
            throw new InvalidOperationException($"数据源 {request.DataSourceId} 不存在");

        var stopwatch = Stopwatch.StartNew();
        
        if (dataSource.Type is "api" or "http")
        {
            return await ExecuteApiQueryAsync(dataSource, request, cancellationToken);
        }
        else
        {
            return await ExecuteDatabaseQueryAsync(dataSource, request, cancellationToken);
        }
    }

    #region 数据库查询

    private async Task<List<Dictionary<string, object?>>> QueryDatabasePreviewAsync(
        ReportDefinition report, 
        Dictionary<string, object?>? parameters, 
        int pageNumber, 
        int pageSize, 
        CancellationToken cancellationToken)
    {
        var query = BuildQuery(report, parameters);
        var connectionString = GetConnectionString(report);
        var dataSourceType = GetDataSourceType(report);
        
        var paginatedQuery = $@"
            SELECT * FROM (
                {query}
            ) AS paged_data
            LIMIT @PageSize OFFSET @Offset;
        ";

        var queryParams = new Dictionary<string, object?>(parameters ?? new Dictionary<string, object?>())
        {
            ["PageSize"] = pageSize,
            ["Offset"] = (pageNumber - 1) * pageSize
        };

        return await ExecuteQueryAsync(connectionString, dataSourceType, paginatedQuery, queryParams, cancellationToken);
    }

    private async IAsyncEnumerable<Dictionary<string, object?>> StreamDatabaseDataAsync(
        ReportDefinition report, 
        Dictionary<string, object?>? parameters,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var query = BuildQuery(report, parameters);
        var connectionString = GetConnectionString(report);
        var dataSourceType = GetDataSourceType(report);

        if (dataSourceType == "mysql")
        {
            await using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);
            
            await using var command = new MySqlCommand(query, connection);
            command.CommandTimeout = 300;
            AddMySqlParameters(command, parameters);
            
            await using var reader = await command.ExecuteReaderAsync(
                CommandBehavior.SequentialAccess | CommandBehavior.SingleResult, cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                yield return ReadDataReaderRow(reader);
            }
        }
        else
        {
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);
            
            await using var command = new NpgsqlCommand(query, connection);
            command.CommandTimeout = 300;
            command.AllResultTypesAreUnknown = true;
            AddNpgsqlParameters(command, parameters);
            
            await using var reader = await command.ExecuteReaderAsync(
                CommandBehavior.SequentialAccess | CommandBehavior.SingleResult, cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                yield return ReadDataReaderRow(reader);
            }
        }
    }

    private async Task<QueryResultResponse> ExecuteDatabaseQueryAsync(
        DataSource dataSource, 
        ExecuteQueryRequest request, 
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        
        var data = new List<Dictionary<string, object?>>();
        var columns = new List<ColumnMeta>();

        if (dataSource.Type == "mysql")
        {
            await using var connection = new MySqlConnection(dataSource.ConnectionString);
            await connection.OpenAsync(cancellationToken);
            
            await using var command = new MySqlCommand(request.Query, connection);
            command.CommandTimeout = 120;
            AddMySqlParameters(command, request.Parameters);

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
                if (count <= 1000)
                {
                    data.Add(ReadDataReaderRow(reader));
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
        else
        {
            await using var connection = new NpgsqlConnection(dataSource.ConnectionString);
            await connection.OpenAsync(cancellationToken);
            
            await using var command = new NpgsqlCommand(request.Query, connection);
            command.CommandTimeout = 120;
            AddNpgsqlParameters(command, request.Parameters);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            
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
                if (count <= 1000)
                {
                    data.Add(ReadDataReaderRow(reader));
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
    }

    private async Task<List<Dictionary<string, object?>>> ExecuteQueryAsync(
        string connectionString, 
        string dataSourceType, 
        string query, 
        Dictionary<string, object?>? parameters, 
        CancellationToken cancellationToken)
    {
        var result = new List<Dictionary<string, object?>>();

        if (dataSourceType == "mysql")
        {
            await using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);
            
            await using var command = new MySqlCommand(query, connection);
            AddMySqlParameters(command, parameters);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                result.Add(ReadDataReaderRow(reader));
            }
        }
        else
        {
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);
            
            await using var command = new NpgsqlCommand(query, connection);
            AddNpgsqlParameters(command, parameters);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                result.Add(ReadDataReaderRow(reader));
            }
        }

        return result;
    }

    private async Task<long> ExecuteScalarAsync(
        string connectionString, 
        string dataSourceType, 
        string query, 
        Dictionary<string, object?>? parameters, 
        CancellationToken cancellationToken)
    {
        if (dataSourceType == "mysql")
        {
            await using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);
            
            await using var command = new MySqlCommand(query, connection);
            AddMySqlParameters(command, parameters);

            var result = await command.ExecuteScalarAsync(cancellationToken);
            return result switch
            {
                long l => l,
                int i => i,
                _ => Convert.ToInt64(result)
            };
        }
        else
        {
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);
            
            await using var command = new NpgsqlCommand(query, connection);
            AddNpgsqlParameters(command, parameters);

            var result = await command.ExecuteScalarAsync(cancellationToken);
            return result is long count ? count : 0;
        }
    }

    #endregion

    #region API 查询

    private bool IsApiDataSource(ReportDefinition report)
    {
        return report.DataSourceType.Equals("api", StringComparison.OrdinalIgnoreCase) ||
               report.DataSourceType.Equals("http", StringComparison.OrdinalIgnoreCase);
    }

    private async Task<List<Dictionary<string, object?>>> QueryApiPreviewAsync(
        ReportDefinition report, 
        Dictionary<string, object?>? parameters, 
        int pageNumber, 
        int pageSize, 
        CancellationToken cancellationToken)
    {
        var config = ParseDataSourceConfig(report.DataSourceConfig);
        var apiUrl = config?.RootElement.GetPropertyOrNull("apiUrl")?.GetString() 
            ?? GetConnectionString(report);
        
        var headers = ParseHeaders(config);
        var method = config?.RootElement.GetPropertyOrNull("method")?.GetString() ?? "GET";

        var result = new List<Dictionary<string, object?>>();
        var skip = (pageNumber - 1) * pageSize;
        var count = 0;

        await foreach (var row in _apiDataSourceService.QueryAsync(apiUrl, method, headers, null, cancellationToken))
        {
            if (count >= skip && result.Count < pageSize)
            {
                result.Add(row);
            }
            count++;
            
            if (result.Count >= pageSize) break;
        }

        return result;
    }

    private async IAsyncEnumerable<Dictionary<string, object?>> StreamApiDataAsync(
        ReportDefinition report, 
        Dictionary<string, object?>? parameters,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var config = ParseDataSourceConfig(report.DataSourceConfig);
        var apiUrl = config?.RootElement.GetPropertyOrNull("apiUrl")?.GetString() 
            ?? GetConnectionString(report);
        
        var headers = ParseHeaders(config);
        var method = config?.RootElement.GetPropertyOrNull("method")?.GetString() ?? "GET";
        var usePagination = config?.RootElement.GetPropertyOrNull("usePagination")?.GetBoolean() ?? false;

        if (usePagination)
        {
            await foreach (var row in _apiDataSourceService.QueryPagedAsync(
                apiUrl, 
                pageParamName: config?.RootElement.GetPropertyOrNull("pageParam")?.GetString() ?? "page",
                pageSizeParamName: config?.RootElement.GetPropertyOrNull("pageSizeParam")?.GetString() ?? "pageSize",
                pageSize: config?.RootElement.GetPropertyOrNull("pageSize")?.GetInt32() ?? 100,
                method: method,
                headers: headers,
                cancellationToken: cancellationToken))
            {
                yield return row;
            }
        }
        else
        {
            await foreach (var row in _apiDataSourceService.QueryAsync(apiUrl, method, headers, null, cancellationToken))
            {
                yield return row;
            }
        }
    }

    private async Task<QueryResultResponse> ExecuteApiQueryAsync(
        DataSource dataSource, 
        ExecuteQueryRequest request, 
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        
        // request.Query 对 API 类型来说是 API 路径
        var apiUrl = dataSource.ConnectionString.TrimEnd('/') + "/" + request.Query.TrimStart('/');
        
        var result = new List<Dictionary<string, object?>>();
        
        await foreach (var row in _apiDataSourceService.QueryAsync(apiUrl, "GET", null, null, cancellationToken))
        {
            result.Add(row);
            if (result.Count >= 1000) break;
        }

        stopwatch.Stop();

        // 提取列元数据
        var columns = new List<ColumnMeta>();
        if (result.Count > 0)
        {
            columns = result[0].Keys.Select(k => new ColumnMeta
            {
                FieldName = k,
                DisplayName = k,
                DataType = "string"
            }).ToList();
        }

        return new QueryResultResponse
        {
            Data = result,
            Columns = columns,
            TotalCount = result.Count,
            QueryExecutionTime = $"{stopwatch.ElapsedMilliseconds}ms"
        };
    }

    #endregion

    #region 工具方法

    private string BuildQuery(ReportDefinition report, Dictionary<string, object?>? parameters)
    {
        if (string.IsNullOrWhiteSpace(report.QueryTemplate))
        {
            throw new InvalidOperationException("报表查询模板为空");
        }

        var query = report.QueryTemplate;
        
        // 简单参数替换
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
                if (config?.TryGetValue("connectionString", out var connStr) == true)
                {
                    return connStr.GetString() ?? string.Empty;
                }
            }
            catch { }
        }

        return _configuration.GetConnectionString("ReportDatabase") 
            ?? _configuration.GetConnectionString("DefaultConnection") 
            ?? throw new InvalidOperationException("未找到连接字符串");
    }

    private string GetDataSourceType(ReportDefinition report)
    {
        if (!string.IsNullOrWhiteSpace(report.DataSourceConfig))
        {
            try
            {
                var config = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(report.DataSourceConfig);
                if (config?.TryGetValue("type", out var type) == true)
                {
                    return type.GetString()?.ToLower() ?? "postgresql";
                }
                if (config?.TryGetValue("dataSourceId", out var dsId) == true && 
                    Guid.TryParse(dsId.GetString(), out var dataSourceId))
                {
                    var ds = _context.DataSources.AsNoTracking().FirstOrDefault(d => d.Id == dataSourceId);
                    if (ds != null) return ds.Type.ToLower();
                }
            }
            catch { }
        }

        return "postgresql";
    }

    private JsonDocument? ParseDataSourceConfig(string? config)
    {
        if (string.IsNullOrWhiteSpace(config)) return null;
        try
        {
            return JsonDocument.Parse(config);
        }
        catch
        {
            return null;
        }
    }

    private Dictionary<string, string?>? ParseHeaders(JsonDocument? config)
    {
        if (config == null) return null;
        
        try
        {
            if (config.RootElement.TryGetProperty("headers", out var headersElement) &&
                headersElement.ValueKind == JsonValueKind.Object)
            {
                return headersElement.EnumerateObject()
                    .ToDictionary(p => p.Name, p => p.Value.GetString());
            }
        }
        catch { }
        
        return null;
    }

    private void AddNpgsqlParameters(NpgsqlCommand command, Dictionary<string, object?>? parameters)
    {
        if (parameters == null) return;

        foreach (var param in parameters)
        {
            var paramName = param.Key.StartsWith("@") ? param.Key : $"@{param.Key}";
            var value = param.Value ?? DBNull.Value;
            command.Parameters.AddWithValue(paramName, value);
        }
    }

    private void AddMySqlParameters(MySqlCommand command, Dictionary<string, object?>? parameters)
    {
        if (parameters == null) return;

        foreach (var param in parameters)
        {
            var paramName = param.Key.StartsWith("@") ? param.Key : $"@{param.Key}";
            var value = param.Value ?? DBNull.Value;
            command.Parameters.AddWithValue(paramName, value);
        }
    }

    private Dictionary<string, object?> ReadDataReaderRow(IDataReader reader)
    {
        var row = new Dictionary<string, object?>();
        for (int i = 0; i < reader.FieldCount; i++)
        {
            var name = reader.GetName(i);
            var value = reader.IsDBNull(i) ? null : reader.GetValue(i);
            row[name] = value;
        }
        return row;
    }

    #endregion
}

/// <summary>
/// JsonElement 扩展方法
/// </summary>
public static class JsonElementExtensions
{
    public static JsonElement? GetPropertyOrNull(this JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var property))
        {
            return property;
        }
        return null;
    }
}
