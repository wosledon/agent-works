using System.Data;
using System.Diagnostics;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using Npgsql;
using OneReport.Data;
using OneReport.Data.Entities;
using OneReport.Models.DTOs;
using OneReport.Models.Responses;
using OneReport.Services.Interfaces;

namespace OneReport.Services.Implementations;

/// <summary>
/// 图表服务实现
/// </summary>
public class ChartService : IChartService
{
    private readonly AppDbContext _context;
    private readonly ILogger<ChartService> _logger;

    public ChartService(
        AppDbContext context,
        ILogger<ChartService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ChartDefinitionDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.ChartDefinitions
            .AsNoTracking()
            .Include(c => c.DataSource)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        return entity == null ? null : MapToDto(entity);
    }

    public async Task<ChartDefinitionListDto> GetListAsync(int pageNumber, int pageSize, string? search = null, CancellationToken cancellationToken = default)
    {
        var query = _context.ChartDefinitions
            .AsNoTracking()
            .Include(c => c.DataSource)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.ToLower();
            query = query.Where(c => 
                c.Name.ToLower().Contains(searchLower) ||
                c.ChartType.ToLower().Contains(searchLower));
        }

        query = query.OrderBy(c => c.Name);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new ChartDefinitionListDto
        {
            Items = items.Select(MapToDto).ToList(),
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
        };
    }

    public async Task<List<ChartDefinitionDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var entities = await _context.ChartDefinitions
            .AsNoTracking()
            .Include(c => c.DataSource)
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .ToListAsync(cancellationToken);

        return entities.Select(MapToDto).ToList();
    }

    public async Task<ChartDefinitionDto> CreateAsync(CreateChartDefinitionDto dto, CancellationToken cancellationToken = default)
    {
        // 验证数据源存在
        var dataSource = await _context.DataSources
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == dto.DataSourceId && d.IsActive, cancellationToken);

        if (dataSource == null)
            throw new InvalidOperationException($"数据源 {dto.DataSourceId} 不存在或已禁用");

        var entity = new ChartDefinition
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            Description = dto.Description,
            ChartType = dto.ChartType.ToLower(),
            DataSourceId = dto.DataSourceId,
            Query = dto.Query,
            Config = dto.Config != null ? JsonSerializer.Serialize(dto.Config) : null,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = Guid.Empty // TODO: 从当前用户获取
        };

        _context.ChartDefinitions.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("图表创建成功: {ChartId} - {Name}, 类型: {Type}", entity.Id, entity.Name, entity.ChartType);

        return MapToDto(entity);
    }

    public async Task<ChartDefinitionDto?> UpdateAsync(Guid id, UpdateChartDefinitionDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await _context.ChartDefinitions.FindAsync(new object[] { id }, cancellationToken);
        if (entity == null) return null;

        // 验证数据源存在
        var dataSource = await _context.DataSources
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == dto.DataSourceId && d.IsActive, cancellationToken);

        if (dataSource == null)
            throw new InvalidOperationException($"数据源 {dto.DataSourceId} 不存在或已禁用");

        entity.Name = dto.Name;
        entity.Description = dto.Description;
        entity.ChartType = dto.ChartType.ToLower();
        entity.DataSourceId = dto.DataSourceId;
        entity.Query = dto.Query;
        entity.Config = dto.Config != null ? JsonSerializer.Serialize(dto.Config) : null;
        entity.IsActive = dto.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("图表更新成功: {ChartId}", id);

        return MapToDto(entity);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.ChartDefinitions.FindAsync(new object[] { id }, cancellationToken);
        if (entity == null) return false;

        _context.ChartDefinitions.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);
        
        _logger.LogInformation("图表删除成功: {ChartId}", id);
        return true;
    }

    public async Task<ChartDataResponse> GetChartDataAsync(Guid chartId, Dictionary<string, object?>? parameters, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        var chart = await _context.ChartDefinitions
            .AsNoTracking()
            .Include(c => c.DataSource)
            .FirstOrDefaultAsync(c => c.Id == chartId && c.IsActive, cancellationToken);

        if (chart == null)
            throw new InvalidOperationException($"图表 {chartId} 不存在或已禁用");

        // 解析配置
        ChartConfig? config = null;
        if (!string.IsNullOrEmpty(chart.Config))
        {
            try
            {
                config = JsonSerializer.Deserialize<ChartConfig>(chart.Config);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "图表配置解析失败: {ChartId}", chartId);
            }
        }

        // 执行查询
        var rawData = await ExecuteQueryAsync(chart.DataSource, chart.Query, parameters, cancellationToken);
        stopwatch.Stop();

        // 转换数据格式
        var response = TransformToChartData(chart.ChartType, rawData, config);
        response.ChartType = chart.ChartType;
        response.TotalCount = rawData.Count;
        response.RawData = rawData.Take(100).ToList(); // 保留部分原始数据用于调试
        response.QueryExecutionTime = $"{stopwatch.ElapsedMilliseconds}ms";

        return response;
    }

    private async Task<List<Dictionary<string, object?>>> ExecuteQueryAsync(
        DataSource dataSource, 
        string query, 
        Dictionary<string, object?>? parameters,
        CancellationToken cancellationToken)
    {
        var result = new List<Dictionary<string, object?>>();

        if (dataSource.Type == "mysql")
        {
            await using var connection = new MySqlConnection(dataSource.ConnectionString);
            await connection.OpenAsync(cancellationToken);
            
            await using var command = new MySqlCommand(query, connection);
            command.CommandTimeout = 30;
            
            if (parameters != null)
            {
                foreach (var param in parameters)
                {
                    var paramName = param.Key.StartsWith("@") ? param.Key : $"@{param.Key}";
                    command.Parameters.AddWithValue(paramName, param.Value ?? DBNull.Value);
                }
            }

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var row = new Dictionary<string, object?>();
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    var name = reader.GetName(i);
                    var value = reader.IsDBNull(i) ? null : reader.GetValue(i);
                    row[name] = value;
                }
                result.Add(row);
            }
        }
        else
        {
            await using var connection = new NpgsqlConnection(dataSource.ConnectionString);
            await connection.OpenAsync(cancellationToken);
            
            await using var command = new NpgsqlCommand(query, connection);
            command.CommandTimeout = 30;
            
            if (parameters != null)
            {
                foreach (var param in parameters)
                {
                    var paramName = param.Key.StartsWith("@") ? param.Key : $"@{param.Key}";
                    command.Parameters.AddWithValue(paramName, param.Value ?? DBNull.Value);
                }
            }

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var row = new Dictionary<string, object?>();
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    var name = reader.GetName(i);
                    var value = reader.IsDBNull(i) ? null : reader.GetValue(i);
                    row[name] = value;
                }
                result.Add(row);
            }
        }

        return result;
    }

    private ChartDataResponse TransformToChartData(string chartType, List<Dictionary<string, object?>> rawData, ChartConfig? config)
    {
        var response = new ChartDataResponse();

        if (rawData.Count == 0 || config == null)
        {
            return response;
        }

        var firstRow = rawData[0];
        var keys = firstRow.Keys.ToList();

        // 根据图表类型转换数据
        switch (chartType.ToLower())
        {
            case "pie":
            case "doughnut":
                response = TransformPieChartData(rawData, config, keys);
                break;
            case "line":
            case "bar":
            case "area":
            default:
                response = TransformLineBarChartData(rawData, config, keys);
                break;
        }

        return response;
    }

    private ChartDataResponse TransformLineBarChartData(List<Dictionary<string, object?>> rawData, ChartConfig config, List<string> keys)
    {
        var response = new ChartDataResponse();

        // 获取 X 轴类别
        if (!string.IsNullOrEmpty(config.XAxisField) && keys.Contains(config.XAxisField))
        {
            response.Categories = rawData
                .Select(row => row[config.XAxisField]?.ToString() ?? "")
                .Distinct()
                .ToList();
        }

        // 生成系列数据
        if (config.YAxisFields != null && config.YAxisFields.Count > 0)
        {
            foreach (var yField in config.YAxisFields.Where(f => keys.Contains(f)))
            {
                var seriesData = rawData.Select(row => row[yField]).ToList();
                
                response.Series.Add(new SeriesData
                {
                    Name = yField,
                    Data = seriesData,
                    Type = config.Series?.FirstOrDefault(s => s.Field == yField)?.Type
                });
            }
        }
        else if (config.Series != null)
        {
            foreach (var seriesConfig in config.Series.Where(s => keys.Contains(s.Field)))
            {
                var seriesData = rawData.Select(row => row[seriesConfig.Field]).ToList();
                
                response.Series.Add(new SeriesData
                {
                    Name = seriesConfig.Name,
                    Data = seriesData,
                    Type = seriesConfig.Type,
                    Color = seriesConfig.Color
                });
            }
        }
        else
        {
            // 自动检测数值列
            var numericFields = keys.Where(k => 
            {
                var value = rawData.FirstOrDefault()?[k];
                return value is int || value is long || value is double || value is decimal || value is float;
            }).ToList();

            foreach (var field in numericFields)
            {
                var seriesData = rawData.Select(row => row[field]).ToList();
                
                response.Series.Add(new SeriesData
                {
                    Name = field,
                    Data = seriesData
                });
            }
        }

        return response;
    }

    private ChartDataResponse TransformPieChartData(List<Dictionary<string, object?>> rawData, ChartConfig config, List<string> keys)
    {
        var response = new ChartDataResponse();

        var nameField = config.CategoryField ?? config.XAxisField;
        var valueField = config.ValueField ?? config.YAxisFields?.FirstOrDefault();

        if (!string.IsNullOrEmpty(nameField) && !string.IsNullOrEmpty(valueField) &&
            keys.Contains(nameField) && keys.Contains(valueField))
        {
            var seriesData = rawData.Select(row => new
            {
                Name = row[nameField]?.ToString() ?? "",
                Value = row[valueField]
            }).ToList();

            response.Series.Add(new SeriesData
            {
                Name = valueField,
                Data = seriesData.Select(x => (object?)x.Value).ToList()
            });

            response.Categories = seriesData.Select(x => x.Name).ToList();
        }

        return response;
    }

    private static ChartDefinitionDto MapToDto(ChartDefinition entity)
    {
        ChartConfig? config = null;
        if (!string.IsNullOrEmpty(entity.Config))
        {
            try
            {
                config = JsonSerializer.Deserialize<ChartConfig>(entity.Config);
            }
            catch { }
        }

        return new ChartDefinitionDto
        {
            Id = entity.Id,
            Name = entity.Name,
            Description = entity.Description,
            ChartType = entity.ChartType,
            DataSourceId = entity.DataSourceId,
            DataSourceName = entity.DataSource?.Name,
            Query = entity.Query,
            Config = config,
            IsActive = entity.IsActive,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }
}
