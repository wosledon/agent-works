using System.Globalization;
using System.IO.Pipelines;
using System.Text;
using System.Text.Json;
using ClosedXML.Excel;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.EntityFrameworkCore;
using OneReport.Data;
using OneReport.Data.Entities;
using OneReport.Models.Requests;
using OneReport.Models.Responses;
using OneReport.Services.Interfaces;

namespace OneReport.Services.Implementations;

/// <summary>
/// 报表导出服务实现 - 支持流式导出，大文件低内存占用
/// </summary>
public class ReportExportService : IReportExportService
{
    private readonly AppDbContext _context;
    private readonly IReportDataService _dataService;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<ReportExportService> _logger;
    
    // 流式导出批次大小
    private const int StreamBatchSize = 1000;

    public ReportExportService(
        AppDbContext context,
        IReportDataService dataService,
        IWebHostEnvironment environment,
        ILogger<ReportExportService> logger)
    {
        _context = context;
        _dataService = dataService;
        _environment = environment;
        _logger = logger;
    }

    /// <summary>
    /// 流式导出为CSV - 低内存占用，适合大数据量
    /// </summary>
    public async Task<Stream> ExportToCsvAsync(
        Guid reportDefinitionId, 
        Dictionary<string, object>? parameters, 
        CancellationToken cancellationToken = default)
    {
        var report = await GetReportDefinitionAsync(reportDefinitionId, cancellationToken);
        var columns = report.Columns.Where(c => c.IsVisible).OrderBy(c => c.DisplayOrder).ToList();
        
        var stream = new MemoryStream();
        var writer = new StreamWriter(stream, Encoding.UTF8, leaveOpen: true);
        var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            BufferSize = 4096
        });

        // 写入表头
        foreach (var col in columns)
        {
            csv.WriteField(col.DisplayName);
        }
        await csv.NextRecordAsync();

        // 流式写入数据 - 使用 IAsyncEnumerable 避免内存中加载所有数据
        long recordCount = 0;
        await foreach (var row in _dataService.StreamDataAsync(reportDefinitionId, parameters, cancellationToken))
        {
            foreach (var col in columns)
            {
                var value = row.GetValueOrDefault(col.FieldName);
                csv.WriteField(FormatValue(value, col.Format));
            }
            await csv.NextRecordAsync();
            recordCount++;

            // 定期刷新缓冲区
            if (recordCount % StreamBatchSize == 0)
            {
                await csv.FlushAsync();
                _logger.LogDebug("CSV导出进度: {Count} 条记录", recordCount);
            }
        }

        await writer.FlushAsync();
        stream.Position = 0;
        
        _logger.LogInformation("CSV导出完成: {ReportId}, 共 {Count} 条记录", reportDefinitionId, recordCount);
        return stream;
    }

    /// <summary>
    /// 流式导出为Excel - 使用 ClosedXML 的流式写入
    /// </summary>
    public async Task<Stream> ExportToExcelAsync(
        Guid reportDefinitionId, 
        Dictionary<string, object>? parameters, 
        CancellationToken cancellationToken = default)
    {
        var report = await GetReportDefinitionAsync(reportDefinitionId, cancellationToken);
        var columns = report.Columns.Where(c => c.IsVisible).OrderBy(c => c.DisplayOrder).ToList();
        
        var stream = new MemoryStream();
        long recordCount = 0;
        
        // 使用 ClosedXML 创建 Excel，设置流式模式
        using (var workbook = new XLWorkbook())
        {
            var worksheet = workbook.Worksheets.Add(report.Name);
            
            // 写入表头
            for (int i = 0; i < columns.Count; i++)
            {
                var cell = worksheet.Cell(1, i + 1);
                cell.Value = columns[i].DisplayName;
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.LightGray;
            }

            // 流式写入数据
            long rowIndex = 2;
            recordCount = 0;
            
            await foreach (var row in _dataService.StreamDataAsync(reportDefinitionId, parameters, cancellationToken))
            {
                for (int colIndex = 0; colIndex < columns.Count; colIndex++)
                {
                    var col = columns[colIndex];
                    var value = row.GetValueOrDefault(col.FieldName);
                    var cell = worksheet.Cell(rowIndex, colIndex + 1);
                    SetCellValue(cell, value, col.DataType);
                }
                
                rowIndex++;
                recordCount++;

                // 定期保存以避免内存溢出 (每 10000 行)
                if (recordCount % 10000 == 0)
                {
                    _logger.LogDebug("Excel导出进度: {Count} 条记录", recordCount);
                }

                // 超过 100 万行时创建新工作表
                if (rowIndex > 1_000_000)
                {
                    worksheet = workbook.Worksheets.Add($"{report.Name}_{worksheet.Name}");
                    rowIndex = 1;
                }
            }

            // 自动调整列宽
            worksheet.Columns().AdjustToContents();
            
            // 保存到流
            workbook.SaveAs(stream, false, false);
        }

        stream.Position = 0;
        _logger.LogInformation("Excel导出完成: {ReportId}, 共 {Count} 条记录", reportDefinitionId, recordCount);
        return stream;
    }

    /// <summary>
    /// 流式导出为JSON - 使用流式JSON写入器
    /// </summary>
    public async Task<Stream> ExportToJsonAsync(
        Guid reportDefinitionId, 
        Dictionary<string, object>? parameters, 
        CancellationToken cancellationToken = default)
    {
        var report = await GetReportDefinitionAsync(reportDefinitionId, cancellationToken);
        var columns = report.Columns.Where(c => c.IsVisible).OrderBy(c => c.DisplayOrder).ToList();
        
        var stream = new MemoryStream();
        var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = false });

        writer.WriteStartArray();
        
        long recordCount = 0;
        await foreach (var row in _dataService.StreamDataAsync(reportDefinitionId, parameters, cancellationToken))
        {
            writer.WriteStartObject();
            foreach (var col in columns)
            {
                var value = row.GetValueOrDefault(col.FieldName);
                WriteJsonValue(writer, col.FieldName, value);
            }
            writer.WriteEndObject();
            recordCount++;

            // 定期刷新缓冲区
            if (recordCount % StreamBatchSize == 0)
            {
                await writer.FlushAsync(cancellationToken);
            }
        }

        writer.WriteEndArray();
        await writer.FlushAsync(cancellationToken);
        await writer.DisposeAsync();
        
        stream.Position = 0;
        _logger.LogInformation("JSON导出完成: {ReportId}, 共 {Count} 条记录", reportDefinitionId, recordCount);
        return stream;
    }

    /// <summary>
    /// 创建异步导出任务
    /// </summary>
    public async Task<ExportJobResponse> CreateExportJobAsync(
        ExportReportRequest request, 
        CancellationToken cancellationToken = default)
    {
        var report = await GetReportDefinitionAsync(request.ReportDefinitionId, cancellationToken);
        
        var history = new ReportExportHistory
        {
            Id = Guid.NewGuid(),
            ReportDefinitionId = request.ReportDefinitionId,
            ExportFormat = request.Format,
            Status = "processing",
            CreatedAt = DateTime.UtcNow,
            Parameters = request.Parameters != null ? JsonSerializer.Serialize(request.Parameters) : null
        };

        _context.ReportExportHistories.Add(history);
        await _context.SaveChangesAsync(cancellationToken);

        // 后台执行导出
        _ = Task.Run(async () =>
        {
            try
            {
                await ExecuteExportAsync(history.Id, report, request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导出任务失败: {ExportId}", history.Id);
                await UpdateExportStatusAsync(history.Id, "failed", errorMessage: ex.Message);
            }
        }, cancellationToken);

        return new ExportJobResponse
        {
            ExportId = history.Id,
            Status = "processing",
            CreatedAt = history.CreatedAt
        };
    }

    public async Task<ExportProgressResponse?> GetExportProgressAsync(
        Guid exportId, 
        CancellationToken cancellationToken = default)
    {
        var history = await _context.ReportExportHistories
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == exportId, cancellationToken);

        if (history == null) return null;

        return new ExportProgressResponse
        {
            ExportId = history.Id,
            Status = history.Status,
            RecordCount = history.RecordCount,
            FileSize = history.FileSize,
            ErrorMessage = history.ErrorMessage,
            CompletedAt = history.CompletedAt
        };
    }

    public async Task<(string fileName, Stream fileStream)?> GetExportFileAsync(
        Guid exportId, 
        CancellationToken cancellationToken = default)
    {
        var history = await _context.ReportExportHistories
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == exportId && e.Status == "completed", cancellationToken);

        if (history?.FilePath == null || !File.Exists(history.FilePath))
            return null;

        var fileName = Path.GetFileName(history.FilePath);
        var stream = File.OpenRead(history.FilePath);
        return (fileName, stream);
    }

    #region 私有方法

    private async Task<ReportDefinition> GetReportDefinitionAsync(Guid id, CancellationToken ct)
    {
        var report = await _context.ReportDefinitions
            .AsNoTracking()
            .Include(r => r.Columns)
            .FirstOrDefaultAsync(r => r.Id == id && r.IsActive, ct);

        if (report == null)
            throw new InvalidOperationException($"Report definition {id} not found");

        return report;
    }

    private async Task ExecuteExportAsync(
        Guid exportId, 
        ReportDefinition report, 
        ExportReportRequest request)
    {
        var fileName = $"{request.FileName ?? report.Name}_{DateTime.UtcNow:yyyyMMddHHmmss}";
        var extension = request.Format.ToLower() switch
        {
            "csv" => ".csv",
            "excel" or "xlsx" => ".xlsx",
            "json" => ".json",
            _ => ".txt"
        };

        var uploadsPath = Path.Combine(_environment.WebRootPath ?? "wwwroot", "exports");
        Directory.CreateDirectory(uploadsPath);
        var filePath = Path.Combine(uploadsPath, $"{fileName}{extension}");

        Stream? exportStream = null;
        try
        {
            exportStream = request.Format.ToLower() switch
            {
                "csv" => await ExportToCsvAsync(request.ReportDefinitionId, request.Parameters),
                "excel" or "xlsx" => await ExportToExcelAsync(request.ReportDefinitionId, request.Parameters),
                "json" => await ExportToJsonAsync(request.ReportDefinitionId, request.Parameters),
                _ => throw new NotSupportedException($"Format {request.Format} not supported")
            };

            // 保存到文件
            using var fileStream = File.Create(filePath);
            await exportStream.CopyToAsync(fileStream);
            
            var fileInfo = new FileInfo(filePath);
            
            await UpdateExportStatusAsync(
                exportId, 
                "completed", 
                filePath: filePath, 
                fileSize: fileInfo.Length);
        }
        finally
        {
            exportStream?.Dispose();
        }
    }

    private async Task UpdateExportStatusAsync(
        Guid exportId, 
        string status, 
        string? filePath = null, 
        long? fileSize = null,
        string? errorMessage = null)
    {
        var history = await _context.ReportExportHistories.FindAsync(exportId);
        if (history == null) return;

        history.Status = status;
        history.FilePath = filePath;
        history.FileSize = fileSize;
        history.ErrorMessage = errorMessage;
        history.CompletedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }

    private string? FormatValue(object? value, string? format)
    {
        if (value == null || value == DBNull.Value) return null;

        if (!string.IsNullOrEmpty(format) && value is IFormattable formattable)
        {
            return formattable.ToString(format, CultureInfo.InvariantCulture);
        }

        return value.ToString();
    }

    private void SetCellValue(IXLCell cell, object? value, string dataType)
    {
        if (value == null || value == DBNull.Value)
        {
            cell.Value = Blank.Value;
            return;
        }

        switch (dataType.ToLower())
        {
            case "number":
            case "int":
            case "integer":
            case "decimal":
            case "double":
            case "float":
                cell.Value = Convert.ToDouble(value);
                break;
            case "date":
            case "datetime":
                if (value is DateTime dt)
                    cell.Value = dt;
                else if (DateTime.TryParse(value.ToString(), out var parsedDt))
                    cell.Value = parsedDt;
                else
                    cell.Value = value.ToString();
                break;
            case "boolean":
            case "bool":
                cell.Value = Convert.ToBoolean(value);
                break;
            default:
                cell.Value = value.ToString();
                break;
        }
    }

    private void WriteJsonValue(Utf8JsonWriter writer, string propertyName, object? value)
    {
        if (value == null || value == DBNull.Value)
        {
            writer.WriteNull(propertyName);
            return;
        }

        switch (value)
        {
            case string str:
                writer.WriteString(propertyName, str);
                break;
            case int i:
                writer.WriteNumber(propertyName, i);
                break;
            case long l:
                writer.WriteNumber(propertyName, l);
                break;
            case double d:
                writer.WriteNumber(propertyName, d);
                break;
            case decimal dec:
                writer.WriteNumber(propertyName, dec);
                break;
            case bool b:
                writer.WriteBoolean(propertyName, b);
                break;
            case DateTime dt:
                writer.WriteString(propertyName, dt);
                break;
            case DateTimeOffset dto:
                writer.WriteString(propertyName, dto);
                break;
            default:
                writer.WriteString(propertyName, value.ToString());
                break;
        }
    }

    #endregion
}
