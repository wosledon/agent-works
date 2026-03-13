using Microsoft.EntityFrameworkCore;
using Npgsql;
using OneReport.Data;
using OneReport.Data.Entities;
using OneReport.Models.DTOs;
using OneReport.Models.Requests;
using OneReport.Services.Interfaces;

namespace OneReport.Services.Implementations;

/// <summary>
/// 数据源服务实现
/// </summary>
public class DataSourceService : IDataSourceService
{
    private readonly AppDbContext _context;
    private readonly ILogger<DataSourceService> _logger;

    public DataSourceService(AppDbContext context, ILogger<DataSourceService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<DataSourceDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.DataSources
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

        return entity == null ? null : MapToDto(entity);
    }

    public async Task<List<DataSourceDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var entities = await _context.DataSources
            .AsNoTracking()
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
            Type = dto.Type,
            ConnectionString = dto.ConnectionString,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.DataSources.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("数据源创建成功: {DataSourceId} - {Name}", entity.Id, entity.Name);

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
        entity.Type = dto.Type;
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

    public async Task<bool> TestConnectionAsync(TestConnectionRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            return request.Type.ToLower() switch
            {
                "postgresql" or "postgres" => await TestPostgreSqlConnectionAsync(request.ConnectionString, cancellationToken),
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

    private static DataSourceDto MapToDto(DataSource entity)
    {
        return new DataSourceDto
        {
            Id = entity.Id,
            Name = entity.Name,
            Type = entity.Type,
            // 隐藏敏感信息
            ConnectionString = MaskConnectionString(entity.ConnectionString),
            IsActive = entity.IsActive,
            CreatedAt = entity.CreatedAt
        };
    }

    private static string MaskConnectionString(string connectionString)
    {
        // 简单掩码处理，生产环境应使用更安全的处理
        if (string.IsNullOrEmpty(connectionString)) return connectionString;
        
        // 掩码密码
        var parts = connectionString.Split(';');
        var maskedParts = parts.Select(part =>
        {
            if (part.Trim().StartsWith("Password=", StringComparison.OrdinalIgnoreCase) ||
                part.Trim().StartsWith("Pwd=", StringComparison.OrdinalIgnoreCase))
            {
                return part.Split('=')[0] + "=***";
            }
            return part;
        });
        
        return string.Join(";", maskedParts);
    }
}
