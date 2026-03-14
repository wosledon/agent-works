using OneReport.Models.DTOs;
using OneReport.Models.Requests;
using OneReport.Models.Responses;

namespace OneReport.Services.Interfaces;

/// <summary>
/// 图表服务接口
/// </summary>
public interface IChartService
{
    Task<ChartDefinitionDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ChartDefinitionListDto> GetListAsync(int pageNumber, int pageSize, string? search = null, CancellationToken cancellationToken = default);
    Task<List<ChartDefinitionDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<ChartDefinitionDto> CreateAsync(CreateChartDefinitionDto dto, CancellationToken cancellationToken = default);
    Task<ChartDefinitionDto?> UpdateAsync(Guid id, UpdateChartDefinitionDto dto, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ChartDataResponse> GetChartDataAsync(Guid chartId, Dictionary<string, object?>? parameters, CancellationToken cancellationToken = default);
}
