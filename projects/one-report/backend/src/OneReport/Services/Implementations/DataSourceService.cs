using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using Npgsql;
using OneReport.Data;
using OneReport.Data.Entities;
using OneReport.Models.DTOs;
using OneReport.Models.Requests;
using OneReport.Services.Interfaces;

namespace OneReport.Services.Implementations;

/// <summary>
/// 数据源服务实现 - 支持 PostgreSQL、MySQL、API 数据源
/// </summary>
public class DataSourceService : IDataSourceService
{
    private readonly AppDbContext _context;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<DataSourceService> _logger;

    public DataSourceService(
        AppDbContext context, 
        IHttpClientFactory httpClientFactory,
        ILogger<DataSourceService> logger)
    {
        _context = context;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<DataSourceDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.DataSources
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

        return entity == null ? null : MapToDto(entity);
    }

    public async Task<DataSourceListDto> GetListAsync(int pageNumber, int pageSize, string? search = null, CancellationToken cancellationToken = default)
    {
        var query = _context.DataSources
            .AsNoTracking()
            .AsQueryable();

        // 搜索筛选
        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.ToLower();
            query = query.Where(d => 
                d.Name.ToLower().Contains(searchLower) ||
                d.Type.ToLower().Contains(searchLower));
        }

        // 排序
        query = query.OrderBy(d => d.Name);

        // 分页
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new DataSourceListDto
        {
            Items = items.Select(MapToDto).ToList(),
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
        };
    }

    public async Task<List<DataSourceDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var entities = await _context.DataSources
            .AsNoTracking()
            .Where(d => d.IsActive)
            .OrderBy(d => d.Name)
            .ToListAsync(cancellationToken);

        return entities.Select(MapToDto).ToList();
    }

    public async Task<DataSourceDto> CreateAsync(CreateDataSourceDto dto, CancellationToken cancellationToken = default)
    {
        // 测试连接
        var canConnect = await TestConnectionAsync(new TestConnectionRequest
        {
            Type = dto.Type,
            ConnectionString = dto.ConnectionString
        }, cancellationToken);

        if (!canConnect)
        {
            throw new InvalidOperationException("无法连接到数据源，请检查连接字符串");
        }

        var entity = new DataSource
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            Type = dto.Type.ToLower(),
            ConnectionString = dto.ConnectionString,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.DataSources.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("数据源创建成功: {DataSourceId} - {Name}, 类型: {Type}", entity.Id, entity.Name, entity.Type);

        return MapToDto(entity);
    }

    public async Task<DataSourceDto?> UpdateAsync(Guid id, UpdateDataSourceDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await _context.DataSources.FindAsync(new object[] { id }, cancellationToken);
        if (entity == null) return null;

        // 如果连接字符串变更，测试新连接
        if (entity.ConnectionString != dto.ConnectionString)
        {
            var canConnect = await TestConnectionAsync(new TestConnectionRequest
            {
                Type = dto.Type,
                ConnectionString = dto.ConnectionString
            }, cancellationToken);

            if (!canConnect)
            {
                throw new InvalidOperationException("无法连接到数据源，请检查连接字符串");
            }
        }

        entity.Name = dto.Name;
        entity.Type = dto.Type.ToLower();
        entity.ConnectionString = dto.ConnectionString;
        entity.IsActive = dto.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("数据源更新成功: {DataSourceId}", id);

        return MapToDto(entity);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.DataSources.FindAsync(new object[] { id }, cancellationToken);
        if (entity == null) return false;

        _context.DataSources.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);
        
        _logger.LogInformation("数据源删除成功: {DataSourceId}", id);
        return true;
    }

    public async Task<bool> TestConnectionByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var dataSource = await _context.DataSources
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

        if (dataSource == null)
            throw new InvalidOperationException($"数据源 {id} 不存在");

        return await TestConnectionAsync(new TestConnectionRequest
        {
            Type = dataSource.Type,
            ConnectionString = dataSource.ConnectionString
        }, cancellationToken);
    }

    public async Task<bool> TestConnectionAsync(TestConnectionRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            return request.Type.ToLower() switch
            {
                "postgresql" or "postgres" => await TestPostgreSqlConnectionAsync(request.ConnectionString, cancellationToken),
                "mysql" => await TestMySqlConnectionAsync(request.ConnectionString, cancellationToken),
                "api" or "http" => await TestApiConnectionAsync(request.ConnectionString, cancellationToken),
                _ => throw new NotSupportedException($"数据源类型 {request.Type} 暂不支持")
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "数据源连接测试失败: {Type}", request.Type);
            return false;
        }
    }

    private async Task<bool> TestPostgreSqlConnectionAsync(string connectionString, CancellationToken cancellationToken)
    {
        try
        {
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);
            await using var command = new NpgsqlCommand("SELECT 1", connection);
            var result = await command.ExecuteScalarAsync(cancellationToken);
            return result != null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "PostgreSQL连接测试失败");
            return false;
        }
    }

    private async Task<bool> TestMySqlConnectionAsync(string connectionString, CancellationToken cancellationToken)
    {
        try
        {
            await using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);
            await using var command = new MySqlCommand("SELECT 1", connection);
            var result = await command.ExecuteScalarAsync(cancellationToken);
            return result != null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "MySQL连接测试失败");
            return false;
        }
    }

    private async Task<bool> TestApiConnectionAsync(string connectionString, CancellationToken cancellationToken)
    {
        try
        {
            // connectionString 对于 API 类型实际上是 API URL
            using var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(10);
            
            var response = await client.GetAsync(connectionString, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "API连接测试失败");
            return false;
        }
    }

    private static DataSourceDto MapToDto(DataSource entity)
    {
        return new DataSourceDto
        {
            Id = entity.Id,
            Name = entity.Name,
            Type = entity.Type,
            // 隐藏敏感信息
            ConnectionString = MaskConnectionString(entity.Type, entity.ConnectionString),
            IsActive = entity.IsActive,
            CreatedAt = entity.CreatedAt
        };
    }

    private static string MaskConnectionString(string type, string connectionString)
    {
        if (string.IsNullOrEmpty(connectionString)) return connectionString;
        
        if (type is "api" or "http")
        {
            // 对于 API URL，只返回部分路径
            try
            {
                var uri = new Uri(connectionString);
                return $"{uri.Scheme}://{uri.Host}/***";
            }
            catch
            {
                return "***";
            }
        }

        // 掩码数据库连接字符串的密码
        var parts = connectionString.Split(';');
        var maskedParts = parts.Select(part =>
        {
            var trimmedPart = part.Trim();
            if (trimmedPart.StartsWith("Password=", StringComparison.OrdinalIgnoreCase) ||
                trimmedPart.StartsWith("Pwd=", StringComparison.OrdinalIgnoreCase) ||
                trimmedPart.StartsWith("password=", StringComparison.OrdinalIgnoreCase))
            {
                var key = trimmedPart.Split('=')[0];
                return $"{key}=***";
            }
            return part;
        });
        
        return string.Join(";", maskedParts);
    }
}
