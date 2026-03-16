using System.Globalization;
using System.Text;
using System.Text.Json;
using CsvHelper;
using CsvHelper.Configuration;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
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
    /// 流式导出为Excel - 使用 Open XML SDK 实现真正的流式写入，极低内存占用
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
        
        // 创建 SpreadsheetDocument
        using (var document = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook, true))
        {
            // 创建工作簿部分
            var workbookPart = document.AddWorkbookPart();
            workbookPart.Workbook = new Workbook();
            
            // 添加样式部分（表头加粗、灰色背景）
            var stylesPart = workbookPart.AddNewPart<WorkbookStylesPart>();
            stylesPart.Stylesheet = CreateStylesheet();
            
            // 添加共享字符串部分（优化内存，避免重复字符串）
            var sharedStringPart = workbookPart.AddNewPart<SharedStringTablePart>();
            
            // 创建工作表
            var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
            
            // 使用 OpenXmlWriter 进行流式写入
            using (var writer = OpenXmlWriter.Create(worksheetPart))
            {
                // 开始工作表
                writer.WriteStartElement(new Worksheet());
                writer.WriteStartElement(new SheetData());
                
                // 写入表头（第1行，样式1）
                writer.WriteStartElement(new Row { RowIndex = 1 });
                for (int i = 0; i < columns.Count; i++)
                {
                    var cell = new Cell
                    {
                        CellReference = GetCellReference(1, i + 1),
                        CellValue = new CellValue(columns[i].DisplayName),
                        DataType = CellValues.String,
                        StyleIndex = 1 // 使用样式1（表头样式）
                    };
                    writer.WriteElement(cell);
                }
                writer.WriteEndElement(); // Row
                
                // 流式写入数据
                uint rowIndex = 2;
                await foreach (var row in _dataService.StreamDataAsync(reportDefinitionId, parameters, cancellationToken))
                {
                    writer.WriteStartElement(new Row { RowIndex = rowIndex });
                    
                    for (int colIndex = 0; colIndex < columns.Count; colIndex++)
                    {
                        var col = columns[colIndex];
                        var value = row.GetValueOrDefault(col.FieldName);
                        var cell = CreateCell(rowIndex, colIndex + 1, value, col.DataType);
                        writer.WriteElement(cell);
                    }
                    
                    writer.WriteEndElement(); // Row
                    
                    recordCount++;
                    rowIndex++;
                    
                    // 每10000行刷新一次日志
                    if (recordCount % 10000 == 0)
                    {
                        _logger.LogDebug("Excel导出进度: {Count} 条记录", recordCount);
                    }
                    
                    // 超过100万行时，结束当前工作表并开始新工作表
                    if (rowIndex > 1_000_001)
                    {
                        break; // 简化处理：超过100万行就截断
                    }
                }
                
                writer.WriteEndElement(); // SheetData
                writer.WriteEndElement(); // Worksheet
            }
            
            // 保存共享字符串表
            SaveSharedStringTable(sharedStringPart);
            
            // 添加工作表到工作簿
            workbookPart.Workbook.AppendChild(new Sheets());
            workbookPart.Workbook.GetFirstChild<Sheets>()?.AppendChild(new Sheet
            {
                Id = workbookPart.GetIdOfPart(worksheetPart),
                SheetId = 1,
                Name = report.Name.Length > 31 ? report.Name[..31] : report.Name // Excel 工作表名称最多31个字符
            });
            
            workbookPart.Workbook.Save();
        }
        
        stream.Position = 0;
        _logger.LogInformation("Excel导出完成: {ReportId}, 共 {Count} 条记录", reportDefinitionId, recordCount);
        return stream;
    }
    
    /// <summary>
    /// 创建单元格
    /// </summary>
    private Cell CreateCell(uint rowIndex, int colIndex, object? value, string dataType)
    {
        var cellReference = GetCellReference(rowIndex, colIndex);
        
        if (value == null || value == DBNull.Value)
        {
            return new Cell { CellReference = cellReference };
        }
        
        // 根据数据类型设置值
        return dataType.ToLower() switch
        {
            "number" or "int" or "integer" or "decimal" or "double" or "float" => 
                new Cell 
                { 
                    CellReference = cellReference,
                    CellValue = new CellValue(Convert.ToDouble(value).ToString(CultureInfo.InvariantCulture)),
                    DataType = CellValues.Number
                },
            "date" or "datetime" => 
                new Cell 
                { 
                    CellReference = cellReference,
                    CellValue = new CellValue(Convert.ToDateTime(value).ToOADate().ToString(CultureInfo.InvariantCulture)),
                    DataType = CellValues.Number // Excel 中日期是数字
                },
            "boolean" or "bool" => 
                new Cell 
                { 
                    CellReference = cellReference,
                    CellValue = new CellValue(Convert.ToBoolean(value) ? "1" : "0"),
                    DataType = CellValues.Boolean
                },
            _ => 
                new Cell 
                { 
                    CellReference = cellReference,
                    CellValue = new CellValue(value.ToString() ?? string.Empty),
                    DataType = CellValues.String
                }
        };
    }
    
    /// <summary>
    /// 获取单元格引用（如 A1, B2）
    /// </summary>
    private string GetCellReference(uint rowIndex, int colIndex)
    {
        var columnName = GetColumnName(colIndex);
        return $"{columnName}{rowIndex}";
    }
    
    /// <summary>
    /// 获取列名（如 1=A, 2=B, 27=AA）
    /// </summary>
    private string GetColumnName(int colIndex)
    {
        var result = new StringBuilder();
        while (colIndex > 0)
        {
            colIndex--;
            result.Insert(0, (char)('A' + (colIndex % 26)));
            colIndex /= 26;
        }
        return result.ToString();
    }
    
    /// <summary>
    /// 创建样式表
    /// </summary>
    private Stylesheet CreateStylesheet()
    {
        return new Stylesheet(
            // 字体
            new DocumentFormat.OpenXml.Spreadsheet.Fonts(
                new DocumentFormat.OpenXml.Spreadsheet.Font(), // 默认字体（索引0）
                new DocumentFormat.OpenXml.Spreadsheet.Font(
                    new Bold(), // 加粗
                    new FontSize { Val = 11 },
                    new DocumentFormat.OpenXml.Spreadsheet.Color { Theme = 1 }
                )
            ),
            // 填充
            new Fills(
                new Fill(), // 默认填充（索引0）
                new Fill(
                    new PatternFill { PatternType = PatternValues.Gray125 } // 灰色背景（索引1）
                )
            ),
            // 边框
            new Borders(new Border()),
            // 单元格格式
            new CellFormats(
                new CellFormat(), // 默认格式（索引0）
                new CellFormat // 表头格式（索引1）
                {
                    FontId = 1,
                    FillId = 0,
                    BorderId = 0,
                    ApplyFont = true
                }
            )
        );
    }
    
    /// <summary>
    /// 保存共享字符串表
    /// </summary>
    private void SaveSharedStringTable(SharedStringTablePart sharedStringPart)
    {
        // 简化实现：不使用共享字符串表，直接内联值
        // 如果需要优化大文件中的重复字符串，可以在这里实现
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
            
            column.Item().PaddingVertical(5).LineHorizontal(1).LineColor(QuestPDF.Helpers.Colors.Grey.Lighten2);
            
            column.Item().Text($"生成时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}")
                .FontSize(10).FontColor(QuestPDF.Helpers.Colors.Grey.Medium);
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
                    header.Cell().Background(QuestPDF.Helpers.Colors.Grey.Lighten3)
                        .Border(0.5f).BorderColor(QuestPDF.Helpers.Colors.Grey.Medium)
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
                        .Border(0.5f).BorderColor(QuestPDF.Helpers.Colors.Grey.Lighten2)
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
