using Microsoft.EntityFrameworkCore;
using OneReport.Data;
using OneReport.Data.Entities;
using OneReport.Models.DTOs;
using OneReport.Models.Responses;
using OneReport.Services.Interfaces;

namespace OneReport.Services.Implementations;

/// <summary>
/// 报表定义服务实现
/// </summary>
public class ReportDefinitionService : IReportDefinitionService
{
    private readonly AppDbContext _context;
    private readonly ILogger<ReportDefinitionService> _logger;

    public ReportDefinitionService(AppDbContext context, ILogger<ReportDefinitionService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ReportDefinitionDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.ReportDefinitions
            .AsNoTracking()
            .Include(r => r.Columns)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

        return entity == null ? null : MapToDto(entity);
    }

    public async Task<PagedResponse<ReportDefinitionDto>> GetListAsync(int pageNumber, int pageSize, string? search = null, CancellationToken cancellationToken = default)
    {
        var query = _context.ReportDefinitions
            .AsNoTracking()
            .Include(r => r.Columns)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(r => r.Name.Contains(search) || (r.Description != null && r.Description.Contains(search)));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResponse<ReportDefinitionDto>
        {
            Items = items.Select(MapToDto).ToList(),
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
            HasPreviousPage = pageNumber > 1,
            HasNextPage = pageNumber * pageSize < totalCount
        };
    }

    public async Task<ReportDefinitionDto> CreateAsync(CreateReportDefinitionDto dto, CancellationToken cancellationToken = default)
    {
        var entity = new ReportDefinition
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            Description = dto.Description,
            DataSourceType = dto.DataSourceType,
            DataSourceConfig = dto.DataSourceConfig,
            QueryTemplate = dto.QueryTemplate,
            Columns = dto.Columns.Select((c, i) => new ReportColumn
            {
                Id = Guid.NewGuid(),
                FieldName = c.FieldName,
                DisplayName = c.DisplayName,
                DataType = c.DataType,
                DisplayOrder = c.DisplayOrder,
                IsVisible = c.IsVisible,
                Format = c.Format,
                Width = c.Width,
                Aggregation = c.Aggregation
            }).ToList()
        };

        _context.ReportDefinitions.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("报表定义创建成功: {ReportId} - {ReportName}", entity.Id, entity.Name);

        return MapToDto(entity);
    }

    public async Task<ReportDefinitionDto?> UpdateAsync(Guid id, UpdateReportDefinitionDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await _context.ReportDefinitions
            .Include(r => r.Columns)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

        if (entity == null) return null;

        entity.Name = dto.Name;
        entity.Description = dto.Description;
        entity.DataSourceType = dto.DataSourceType;
        entity.DataSourceConfig = dto.DataSourceConfig;
        entity.QueryTemplate = dto.QueryTemplate;
        entity.IsActive = dto.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;

        // 处理列的更新
        var existingIds = entity.Columns.Select(c => c.Id).ToHashSet();
        var updatedIds = dto.Columns.Where(c => c.Id.HasValue).Select(c => c.Id!.Value).ToHashSet();
        
        // 删除已移除的列
        var toRemove = entity.Columns.Where(c => !updatedIds.Contains(c.Id)).ToList();
        _context.ReportColumns.RemoveRange(toRemove);

        // 更新或添加列
        foreach (var colDto in dto.Columns)
        {
            if (colDto.Id.HasValue && existingIds.Contains(colDto.Id.Value))
            {
                var col = entity.Columns.First(c => c.Id == colDto.Id.Value);
                col.FieldName = colDto.FieldName;
                col.DisplayName = colDto.DisplayName;
                col.DataType = colDto.DataType;
                col.DisplayOrder = colDto.DisplayOrder;
                col.IsVisible = colDto.IsVisible;
                col.Format = colDto.Format;
                col.Width = colDto.Width;
                col.Aggregation = colDto.Aggregation;
            }
            else
            {
                entity.Columns.Add(new ReportColumn
                {
                    Id = Guid.NewGuid(),
                    FieldName = colDto.FieldName,
                    DisplayName = colDto.DisplayName,
                    DataType = colDto.DataType,
                    DisplayOrder = colDto.DisplayOrder,
                    IsVisible = colDto.IsVisible,
                    Format = colDto.Format,
                    Width = colDto.Width,
                    Aggregation = colDto.Aggregation
                });
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("报表定义更新成功: {ReportId}", id);

        return MapToDto(entity);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.ReportDefinitions.FindAsync(new object[] { id }, cancellationToken);
        if (entity == null) return false;

        _context.ReportDefinitions.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);
        
        _logger.LogInformation("报表定义删除成功: {ReportId}", id);
        return true;
    }

    private static ReportDefinitionDto MapToDto(ReportDefinition entity)
    {
        return new ReportDefinitionDto
        {
            Id = entity.Id,
            Name = entity.Name,
            Description = entity.Description,
            DataSourceType = entity.DataSourceType,
            DataSourceConfig = entity.DataSourceConfig,
            QueryTemplate = entity.QueryTemplate,
            IsActive = entity.IsActive,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt,
            Columns = entity.Columns
                .OrderBy(c => c.DisplayOrder)
                .Select(c => new ReportColumnDto
                {
                    Id = c.Id,
                    FieldName = c.FieldName,
                    DisplayName = c.DisplayName,
                    DataType = c.DataType,
                    DisplayOrder = c.DisplayOrder,
                    IsVisible = c.IsVisible,
                    Format = c.Format,
                    Width = c.Width,
                    Aggregation = c.Aggregation
                }).ToList()
        };
    }
}
