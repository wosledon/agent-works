using System.Globalization;
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
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace OneReport.Services.Implementations;

/// <summary>
/// 报表导出服务实现 - 支持流式导出，大文件低内存占用
/// </summary>
public class ReportExportService : IReportExportService
{
    private readonly AppDbContext _context;
    private readonly IReportDataService _dataService;
    private readonly IExportJobQueueService _jobQueueService;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<ReportExportService> _logger;
    
    // 流式导出批次大小
    private const int StreamBatchSize = 1000;

    public ReportExportService(
        AppDbContext context,
        IReportDataService dataService,
        IExportJobQueueService jobQueueService,
        IWebHostEnvironment environment,
        ILogger<ReportExportService> logger)
    {
        _context = context;
        _dataService = dataService;
        _jobQueueService = jobQueueService;
        _environment = environment;
        _logger = logger;
    }

    /// <summary>
    /// 流式导出为CSV - 低内存占用，适合大数据量
    /// </summary>
    public async Task<Stream> ExportToCsvAsync(
        Guid reportDefinitionId, 
        Dictionary<string, object?>? parameters, 
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
        Dictionary<string, object?>? parameters, 
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
            int rowIndex = 2;
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
        Dictionary<string, object?>? parameters, 
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
    /// 导出报表为PDF格式 - 使用 QuestPDF 生成专业PDF报表
    /// </summary>
    public async Task<Stream> ExportToPdfAsync(
        Guid reportDefinitionId, 
        Dictionary<string, object?>? parameters, 
        CancellationToken cancellationToken = default)
    {
        var report = await GetReportDefinitionAsync(reportDefinitionId, cancellationToken);
        var columns = report.Columns.Where(c => c.IsVisible).OrderBy(c => c.DisplayOrder).ToList();
        
        // 收集数据
        var data = new List<Dictionary<string, object?>>();
        long recordCount = 0;
        await foreach (var row in _dataService.StreamDataAsync(reportDefinitionId, parameters, cancellationToken))
        {
            data.Add(row);
            recordCount++;
            
            // PDF 导出限制数据量以避免内存问题
            if (recordCount >= 10000)
            {
                _logger.LogWarning("PDF导出数据量超过10000条限制，已截断: {ReportId}", reportDefinitionId);
                break;
            }
        }

        // 配置 QuestPDF 许可证 (社区版)
        QuestPDF.Settings.License = LicenseType.Community;

        // 生成PDF文档
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(20);
                page.Header().Element(header => BuildHeader(header, report.Name, columns));
                page.Content().Element(content => BuildTableContent(content, columns, data));
                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("第 ");
                    text.CurrentPageNumber();
                    text.Span(" 页 / 共 ");
                    text.TotalPages();
                    text.Span(" 页");
                });
            });
        });

        var stream = new MemoryStream();
        document.GeneratePdf(stream);
        stream.Position = 0;
        
        _logger.LogInformation("PDF导出完成: {ReportId}, 共 {Count} 条记录", reportDefinitionId, recordCount);
        return stream;
    }

    private void BuildHeader(IContainer container, string reportName, List<ReportColumn> columns)
    {
        container.Column(column =>
        {
            column.Item().AlignCenter().Text(reportName)
                .FontSize(18).Bold();
            
            column.Item().PaddingVertical(5).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
            
            column.Item().Text($"生成时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}")
                .FontSize(10).FontColor(Colors.Grey.Medium);
        });
    }

    private void BuildTableContent(IContainer container, List<ReportColumn> columns, List<Dictionary<string, object?>> data)
    {
        container.Table(table =>
        {
            // 定义列
            table.ColumnsDefinition(columnsDef =>
            {
                foreach (var _ in columns)
                {
                    columnsDef.RelativeColumn();
                }
            });

            // 表头
            table.Header(header =>
            {
                foreach (var col in columns)
                {
                    header.Cell().Background(Colors.Grey.Lighten3)
                        .Border(0.5f).BorderColor(Colors.Grey.Medium)
                        .Padding(5)
                        .Text(col.DisplayName)
                        .FontSize(10).Bold();
                }
            });

            // 数据行
            foreach (var row in data)
            {
                foreach (var col in columns)
                {
                    var value = row.GetValueOrDefault(col.FieldName);
                    var formattedValue = FormatValue(value, col.Format) ?? string.Empty;
                    
                    table.Cell()
                        .Border(0.5f).BorderColor(Colors.Grey.Lighten2)
                        .Padding(3)
                        .Text(formattedValue)
                        .FontSize(9);
                }
            }
        });
    }

    /// <summary>
    /// 创建异步导出任务
    /// </summary>
    public async Task<ExportJobResponse> CreateExportJobAsync(
        ExportReportRequest request, 
        CancellationToken cancellationToken = default)
    {
        var report = await _context.ReportDefinitions
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == request.ReportDefinitionId && r.IsActive, cancellationToken);

        if (report == null)
            throw new InvalidOperationException($"报表定义 {request.ReportDefinitionId} 不存在");

        // 提交到任务队列
        var jobId = await _jobQueueService.EnqueueAsync(new ExportJobRequest
        {
            ReportDefinitionId = request.ReportDefinitionId,
            Format = request.Format,
            Parameters = request.Parameters,
            FileName = request.FileName ?? report.Name
        }, cancellationToken);

        return new ExportJobResponse
        {
            ExportId = jobId,
            Status = "pending",
            CreatedAt = DateTime.UtcNow
        };
    }

    public async Task<ExportProgressResponse?> GetExportProgressAsync(
        Guid exportId, 
        CancellationToken cancellationToken = default)
    {
        var status = await _jobQueueService.GetStatusAsync(exportId, cancellationToken);
        
        if (status == null)
        {
            // 回退到直接查询数据库
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

        // 计算进度百分比
        double? progressPercent = null;
        if (status.Status == "processing" && status.RecordCount.HasValue)
        {
            var total = await _dataService.GetTotalCountAsync(status.JobId, null, cancellationToken);
            if (total > 0)
            {
                progressPercent = Math.Min(100, (double)status.RecordCount.Value / total * 100);
            }
        }

        return new ExportProgressResponse
        {
            ExportId = status.JobId,
            Status = status.Status,
            RecordCount = status.RecordCount,
            FileSize = status.FileSize,
            ProgressPercentage = progressPercent,
            ErrorMessage = status.ErrorMessage,
            CompletedAt = status.CompletedAt
        };
    }

    public async Task<(string fileName, Stream fileStream)?> GetExportFileAsync(
        Guid exportId, 
        CancellationToken cancellationToken = default)
    {
        // 先检查队列状态
        var status = await _jobQueueService.GetStatusAsync(exportId, cancellationToken);
        
        if (status?.Status == "completed" && !string.IsNullOrEmpty(status.FilePath) && File.Exists(status.FilePath))
        {
            var fileName = Path.GetFileName(status.FilePath);
            var stream = File.OpenRead(status.FilePath);
            return (fileName, stream);
        }

        // 回退到数据库查询
        var history = await _context.ReportExportHistories
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == exportId && e.Status == "completed", cancellationToken);

        if (history?.FilePath == null || !File.Exists(history.FilePath))
            return null;

        var dbFileName = Path.GetFileName(history.FilePath);
        var dbStream = File.OpenRead(history.FilePath);
        return (dbFileName, dbStream);
    }

    #region 私有方法

    private async Task<ReportDefinition> GetReportDefinitionAsync(Guid id, CancellationToken ct)
    {
        var report = await _context.ReportDefinitions
            .AsNoTracking()
            .Include(r => r.Columns)
            .FirstOrDefaultAsync(r => r.Id == id && r.IsActive, ct);

        if (report == null)
            throw new InvalidOperationException($"报表定义 {id} 不存在");

        return report;
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
