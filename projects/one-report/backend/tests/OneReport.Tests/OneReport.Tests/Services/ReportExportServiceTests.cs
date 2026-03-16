using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using OneReport.Data.Entities;
using OneReport.Models.Requests;
using OneReport.Models.Responses;
using OneReport.Services.Implementations;
using OneReport.Services.Interfaces;
using System.Text;
using System.Text.Json;

namespace OneReport.Tests.Services;

public class ReportExportServiceTests : TestBase
{
    private readonly Mock<IReportDataService> _dataServiceMock;
    private readonly Mock<IExportJobQueueService> _jobQueueMock;
    private readonly Mock<IWebHostEnvironment> _environmentMock;
    private readonly Mock<ILogger<ReportExportService>> _loggerMock;
    private readonly ReportExportService _service;

    public ReportExportServiceTests()
    {
        _dataServiceMock = new Mock<IReportDataService>();
        _jobQueueMock = new Mock<IExportJobQueueService>();
        _environmentMock = new Mock<IWebHostEnvironment>();
        _loggerMock = new Mock<ILogger<ReportExportService>>();

        _environmentMock.Setup(e => e.WebRootPath).Returns(Path.Combine(Path.GetTempPath(), "wwwroot"));
        _environmentMock.Setup(e => e.ContentRootPath).Returns(Path.GetTempPath());

        _service = new ReportExportService(
            Context,
            _dataServiceMock.Object,
            _jobQueueMock.Object,
            _environmentMock.Object,
            _loggerMock.Object);
    }

    #region ExportToCsvAsync

    [Fact]
    public async Task ExportToCsvAsync_ValidReport_ReturnsCsvStream()
    {
        // Arrange
        var report = CreateReportWithColumns();
        Context.ReportDefinitions.Add(report);
        await Context.SaveChangesAsync();

        var testData = CreateTestData(5);
        _dataServiceMock.Setup(d => d.StreamDataAsync(report.Id, null, It.IsAny<CancellationToken>()))
            .Returns(CreateAsyncEnumerable(testData));

        // Act
        var stream = await _service.ExportToCsvAsync(report.Id, null);

        // Assert
        stream.Should().NotBeNull();
        stream.Position = 0;
        using var reader = new StreamReader(stream, Encoding.UTF8);
        var content = await reader.ReadToEndAsync();
        content.Should().Contain("Name,Age,Email");
        content.Should().Contain("User 1,21,user1@test.com");
    }

    [Fact]
    public async Task ExportToCsvAsync_EmptyData_ReturnsHeaderOnly()
    {
        // Arrange
        var report = CreateReportWithColumns();
        Context.ReportDefinitions.Add(report);
        await Context.SaveChangesAsync();

        _dataServiceMock.Setup(d => d.StreamDataAsync(report.Id, null, It.IsAny<CancellationToken>()))
            .Returns(CreateAsyncEnumerable(new List<Dictionary<string, object?>>()));

        // Act
        var stream = await _service.ExportToCsvAsync(report.Id, null);

        // Assert
        stream.Position = 0;
        using var reader = new StreamReader(stream, Encoding.UTF8);
        var content = await reader.ReadToEndAsync();
        content.Should().Contain("Name,Age,Email");
        content.Should().NotContain("Alice");
    }

    [Fact]
    public async Task ExportToCsvAsync_LargeData_HandlesCorrectly()
    {
        // Arrange
        var report = CreateReportWithColumns();
        Context.ReportDefinitions.Add(report);
        await Context.SaveChangesAsync();

        var testData = CreateTestData(5000);
        _dataServiceMock.Setup(d => d.StreamDataAsync(report.Id, null, It.IsAny<CancellationToken>()))
            .Returns(CreateAsyncEnumerable(testData));

        // Act
        var stream = await _service.ExportToCsvAsync(report.Id, null);

        // Assert
        stream.Should().NotBeNull();
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("CSV导出完成")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region ExportToExcelAsync

    [Fact]
    public async Task ExportToExcelAsync_ValidReport_ReturnsExcelStream()
    {
        // Arrange
        var report = CreateReportWithColumns();
        Context.ReportDefinitions.Add(report);
        await Context.SaveChangesAsync();

        var testData = CreateTestData(5);
        _dataServiceMock.Setup(d => d.StreamDataAsync(report.Id, null, It.IsAny<CancellationToken>()))
            .Returns(CreateAsyncEnumerable(testData));

        // Act
        var stream = await _service.ExportToExcelAsync(report.Id, null);

        // Assert
        stream.Should().NotBeNull();
        stream.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task ExportToExcelAsync_EmptyData_ReturnsValidExcel()
    {
        // Arrange
        var report = CreateReportWithColumns();
        Context.ReportDefinitions.Add(report);
        await Context.SaveChangesAsync();

        _dataServiceMock.Setup(d => d.StreamDataAsync(report.Id, null, It.IsAny<CancellationToken>()))
            .Returns(CreateAsyncEnumerable(new List<Dictionary<string, object?>>()));

        // Act
        var stream = await _service.ExportToExcelAsync(report.Id, null);

        // Assert
        stream.Should().NotBeNull();
        stream.Length.Should().BeGreaterThan(0);
    }

    #endregion

    #region ExportToJsonAsync

    [Fact]
    public async Task ExportToJsonAsync_ValidReport_ReturnsJsonArray()
    {
        // Arrange
        var report = CreateReportWithColumns();
        Context.ReportDefinitions.Add(report);
        await Context.SaveChangesAsync();

        var testData = CreateTestData(3);
        _dataServiceMock.Setup(d => d.StreamDataAsync(report.Id, null, It.IsAny<CancellationToken>()))
            .Returns(CreateAsyncEnumerable(testData));

        // Act
        var stream = await _service.ExportToJsonAsync(report.Id, null);

        // Assert
        stream.Should().NotBeNull();
        stream.Position = 0;
        using var reader = new StreamReader(stream);
        var content = await reader.ReadToEndAsync();
        
        var jsonArray = JsonSerializer.Deserialize<List<Dictionary<string, JsonElement>>>(content);
        jsonArray.Should().NotBeNull();
        jsonArray.Should().HaveCount(3);
    }

    [Fact]
    public async Task ExportToJsonAsync_EmptyData_ReturnsEmptyArray()
    {
        // Arrange
        var report = CreateReportWithColumns();
        Context.ReportDefinitions.Add(report);
        await Context.SaveChangesAsync();

        _dataServiceMock.Setup(d => d.StreamDataAsync(report.Id, null, It.IsAny<CancellationToken>()))
            .Returns(CreateAsyncEnumerable(new List<Dictionary<string, object?>>()));

        // Act
        var stream = await _service.ExportToJsonAsync(report.Id, null);

        // Assert
        stream.Position = 0;
        using var reader = new StreamReader(stream);
        var content = await reader.ReadToEndAsync();
        content.Should().Be("[]");
    }

    #endregion

    #region ExportToPdfAsync

    [Fact]
    public async Task ExportToPdfAsync_ValidReport_ReturnsPdfStream()
    {
        // Arrange
        var report = CreateReportWithColumns();
        Context.ReportDefinitions.Add(report);
        await Context.SaveChangesAsync();

        var testData = CreateTestData(5);
        _dataServiceMock.Setup(d => d.StreamDataAsync(report.Id, null, It.IsAny<CancellationToken>()))
            .Returns(CreateAsyncEnumerable(testData));

        // Act
        var stream = await _service.ExportToPdfAsync(report.Id, null);

        // Assert
        stream.Should().NotBeNull();
        stream.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task ExportToPdfAsync_LimitsTo10000Records()
    {
        // Arrange
        var report = CreateReportWithColumns();
        Context.ReportDefinitions.Add(report);
        await Context.SaveChangesAsync();

        // 创建超过 10000 条的数据
        var largeData = CreateTestData(12000);
        _dataServiceMock.Setup(d => d.StreamDataAsync(report.Id, null, It.IsAny<CancellationToken>()))
            .Returns(CreateAsyncEnumerable(largeData));

        // Act
        var stream = await _service.ExportToPdfAsync(report.Id, null);

        // Assert
        stream.Should().NotBeNull();
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("10000条限制")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ExportToPdfAsync_EmptyData_ReturnsValidPdf()
    {
        // Arrange
        var report = CreateReportWithColumns();
        Context.ReportDefinitions.Add(report);
        await Context.SaveChangesAsync();

        _dataServiceMock.Setup(d => d.StreamDataAsync(report.Id, null, It.IsAny<CancellationToken>()))
            .Returns(CreateAsyncEnumerable(new List<Dictionary<string, object?>>()));

        // Act
        var stream = await _service.ExportToPdfAsync(report.Id, null);

        // Assert
        stream.Should().NotBeNull();
        stream.Length.Should().BeGreaterThan(0);
    }

    #endregion

    #region CreateExportJobAsync

    [Fact]
    public async Task CreateExportJobAsync_ValidRequest_CreatesJob()
    {
        // Arrange
        var report = TestDataBuilder.CreateReportDefinition();
        Context.ReportDefinitions.Add(report);
        await Context.SaveChangesAsync();

        var jobId = Guid.NewGuid();
        _jobQueueMock.Setup(j => j.EnqueueAsync(It.IsAny<ExportJobRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(jobId);

        var request = new ExportReportRequest
        {
            ReportDefinitionId = report.Id,
            Format = "csv",
            FileName = "test-export"
        };

        // Act
        var result = await _service.CreateExportJobAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.ExportId.Should().Be(jobId);
        result.Status.Should().Be("pending");
        _jobQueueMock.Verify(j => j.EnqueueAsync(It.Is<ExportJobRequest>(
            r => r.ReportDefinitionId == report.Id && r.Format == "csv"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateExportJobAsync_ReportNotFound_ThrowsException()
    {
        // Arrange
        var request = new ExportReportRequest
        {
            ReportDefinitionId = Guid.NewGuid(),
            Format = "csv"
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CreateExportJobAsync(request));
    }

    #endregion

    #region GetExportProgressAsync

    [Fact]
    public async Task GetExportProgressAsync_ExistingJob_ReturnsStatus()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        var status = new ExportJobStatus
        {
            JobId = jobId,
            Status = "completed",
            RecordCount = 100,
            FileSize = 1024
        };

        _jobQueueMock.Setup(j => j.GetStatusAsync(jobId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(status);

        // Act
        var result = await _service.GetExportProgressAsync(jobId);

        // Assert
        result.Should().NotBeNull();
        result!.Status.Should().Be("completed");
        result.RecordCount.Should().Be(100);
        result.FileSize.Should().Be(1024);
    }

    [Fact]
    public async Task GetExportProgressAsync_JobNotFound_ReturnsNull()
    {
        // Arrange
        _jobQueueMock.Setup(j => j.GetStatusAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ExportJobStatus?)null);

        // Act
        var result = await _service.GetExportProgressAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetExportFileAsync

    [Fact]
    public async Task GetExportFileAsync_CompletedJob_ReturnsFile()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        var tempFile = Path.Combine(Path.GetTempPath(), $"{jobId}.csv");
        await File.WriteAllTextAsync(tempFile, "test content");

        var status = new ExportJobStatus
        {
            JobId = jobId,
            Status = "completed",
            FilePath = tempFile
        };

        _jobQueueMock.Setup(j => j.GetStatusAsync(jobId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(status);

        try
        {
            // Act
            var result = await _service.GetExportFileAsync(jobId);

            // Assert
            result.Should().NotBeNull();
            result!.Value.fileName.Should().Be($"{jobId}.csv");
            result!.Value.fileStream.Should().NotBeNull();
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task GetExportFileAsync_JobNotFound_ReturnsNull()
    {
        // Arrange
        _jobQueueMock.Setup(j => j.GetStatusAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ExportJobStatus?)null);

        // Act
        var result = await _service.GetExportFileAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region Helper Methods

    private ReportDefinition CreateReportWithColumns()
    {
        return new ReportDefinition
        {
            Id = Guid.NewGuid(),
            Name = "Test Report",
            DataSourceType = "sql",
            QueryTemplate = "SELECT * FROM test",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Columns = new List<ReportColumn>
            {
                new() { Id = Guid.NewGuid(), FieldName = "Name", DisplayName = "Name", DataType = "string", IsVisible = true, DisplayOrder = 0 },
                new() { Id = Guid.NewGuid(), FieldName = "Age", DisplayName = "Age", DataType = "number", IsVisible = true, DisplayOrder = 1 },
                new() { Id = Guid.NewGuid(), FieldName = "Email", DisplayName = "Email", DataType = "string", IsVisible = true, DisplayOrder = 2 }
            }
        };
    }

    private List<Dictionary<string, object?>> CreateTestData(int count)
    {
        return Enumerable.Range(1, count).Select(i => new Dictionary<string, object?>
        {
            ["Name"] = $"User {i}",
            ["Age"] = 20 + i,
            ["Email"] = $"user{i}@test.com"
        }).ToList();
    }

    private async IAsyncEnumerable<T> CreateAsyncEnumerable<T>(List<T> items)
    {
        await Task.Yield(); // 避免同步警告
        foreach (var item in items)
        {
            yield return item;
        }
    }

    #endregion
}
