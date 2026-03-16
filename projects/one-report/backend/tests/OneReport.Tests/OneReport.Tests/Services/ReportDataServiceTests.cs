using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using OneReport.Data.Entities;
using OneReport.Models.Requests;
using OneReport.Models.Responses;
using OneReport.Services.Implementations;
using OneReport.Services.Interfaces;
using System.Text.Json;

namespace OneReport.Tests.Services;

public class ReportDataServiceTests : TestBase
{
    private readonly Mock<IApiDataSourceService> _apiDataSourceMock;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly Mock<ILogger<ReportDataService>> _loggerMock;
    private readonly Mock<IQueryResultCacheService> _cacheServiceMock;
    private readonly Mock<IReportExecutionLogService> _logServiceMock;
    private readonly ReportDataService _service;

    public ReportDataServiceTests()
    {
        _apiDataSourceMock = new Mock<IApiDataSourceService>();
        _configurationMock = new Mock<IConfiguration>();
        _loggerMock = new Mock<ILogger<ReportDataService>>();
        _cacheServiceMock = new Mock<IQueryResultCacheService>();
        _logServiceMock = new Mock<IReportExecutionLogService>();

        // 使用索引器设置连接字符串
        _configurationMock.SetupGet(c => c[It.Is<string>(s => s == "ConnectionStrings:DefaultConnection")])
            .Returns("Host=localhost;Database=test;");
        _configurationMock.SetupGet(c => c[It.Is<string>(s => s == "ConnectionStrings:ReportDatabase")])
            .Returns("Host=localhost;Database=test;");

        _service = new ReportDataService(
            Context,
            _apiDataSourceMock.Object,
            _configurationMock.Object,
            _loggerMock.Object,
            _cacheServiceMock.Object,
            _logServiceMock.Object);
    }

    #region PreviewAsync

    [Fact]
    public async Task PreviewAsync_ReportNotFound_ThrowsException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.PreviewAsync(Guid.NewGuid(), null, 1, 10));
    }

    [Fact]
    public async Task PreviewAsync_InactiveReport_ThrowsException()
    {
        // Arrange
        var report = TestDataBuilder.CreateReportDefinition();
        report.IsActive = false;
        Context.ReportDefinitions.Add(report);
        await Context.SaveChangesAsync();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.PreviewAsync(report.Id, null, 1, 10));
    }

    [Fact]
    public async Task PreviewAsync_UsesCache_WhenCacheHit()
    {
        // Arrange
        var report = CreateApiReportDefinition();
        Context.ReportDefinitions.Add(report);
        await Context.SaveChangesAsync();

        var cachedResult = new ReportPreviewResponse
        {
            Data = new List<Dictionary<string, object?>>
            {
                new() { ["name"] = "Cached" }
            },
            Columns = new List<ColumnMeta> { new() { FieldName = "name", DisplayName = "Name" } },
            TotalCount = 1
        };

        _cacheServiceMock.Setup(c => c.GetCachedResultAsync(report.Id, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cachedResult);

        _logServiceMock.Setup(l => l.BeginExecutionAsync(report.Id, "preview", null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Guid.NewGuid());

        // Act
        var result = await _service.PreviewAsync(report.Id, null, 1, 10);

        // Assert
        result.Should().NotBeNull();
        result.Data.Should().HaveCount(1);
        result.Data[0]["name"].Should().Be("Cached");
        _apiDataSourceMock.Verify(a => a.QueryAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string?>>(), null, It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task PreviewAsync_LogsExecution_StartsAndCompletes()
    {
        // Arrange
        var report = CreateApiReportDefinition();
        Context.ReportDefinitions.Add(report);
        await Context.SaveChangesAsync();

        var logId = Guid.NewGuid();
        _logServiceMock.Setup(l => l.BeginExecutionAsync(report.Id, "preview", null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(logId);

        _apiDataSourceMock.Setup(a => a.QueryAsync(It.IsAny<string>(), "GET", null, null, It.IsAny<CancellationToken>()))
            .Returns(CreateAsyncEnumerable(new List<Dictionary<string, object?>>()));

        // Act
        await _service.PreviewAsync(report.Id, null, 1, 10);

        // Assert
        _logServiceMock.Verify(l => l.BeginExecutionAsync(report.Id, "preview", null, null, It.IsAny<CancellationToken>()), Times.Once);
        _logServiceMock.Verify(l => l.CompleteExecutionAsync(logId, It.IsAny<long?>(), null, null, false, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PreviewAsync_Exception_LogsFailure()
    {
        // Arrange
        var report = CreateApiReportDefinition();
        Context.ReportDefinitions.Add(report);
        await Context.SaveChangesAsync();

        var logId = Guid.NewGuid();
        _logServiceMock.Setup(l => l.BeginExecutionAsync(report.Id, "preview", null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(logId);

        _apiDataSourceMock.Setup(a => a.QueryAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string?>>(), null, It.IsAny<CancellationToken>()))
            .Returns(CreateFailingAsyncEnumerable(new Exception("API Error")));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _service.PreviewAsync(report.Id, null, 1, 10));
        _logServiceMock.Verify(l => l.FailExecutionAsync(logId, "API Error", It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region StreamDataAsync

    [Fact]
    public async Task StreamDataAsync_ReportNotFound_ThrowsException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await foreach (var _ in _service.StreamDataAsync(Guid.NewGuid(), null))
            {
            }
        });
    }

    [Fact]
    public async Task StreamDataAsync_ApiDataSource_StreamsData()
    {
        // Arrange
        var report = CreateApiReportDefinition();
        Context.ReportDefinitions.Add(report);
        await Context.SaveChangesAsync();

        var testData = new List<Dictionary<string, object?>>
        {
            new() { ["id"] = 1, ["name"] = "Item 1" },
            new() { ["id"] = 2, ["name"] = "Item 2" },
            new() { ["id"] = 3, ["name"] = "Item 3" }
        };

        _apiDataSourceMock.Setup(a => a.QueryAsync(It.IsAny<string>(), "GET", null, null, It.IsAny<CancellationToken>()))
            .Returns(CreateAsyncEnumerable(testData));

        // Act
        var result = new List<Dictionary<string, object?>>();
        await foreach (var row in _service.StreamDataAsync(report.Id, null))
        {
            result.Add(row);
        }

        // Assert
        result.Should().HaveCount(3);
        result[0]["id"].Should().Be(1);
        result[2]["name"].Should().Be("Item 3");
    }

    #endregion

    #region GetTotalCountAsync

    [Fact]
    public async Task GetTotalCountAsync_ReportNotFound_ReturnsZero()
    {
        // Act
        var result = await _service.GetTotalCountAsync(Guid.NewGuid(), null);

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public async Task GetTotalCountAsync_ApiDataSource_ReturnsNegativeOne()
    {
        // Arrange
        var report = CreateApiReportDefinition();
        Context.ReportDefinitions.Add(report);
        await Context.SaveChangesAsync();

        // Act
        var result = await _service.GetTotalCountAsync(report.Id, null);

        // Assert
        result.Should().Be(-1);
    }

    #endregion

    #region ExecuteQueryAsync

    [Fact]
    public async Task ExecuteQueryAsync_DataSourceNotFound_ThrowsException()
    {
        // Arrange
        var request = new ExecuteQueryRequest
        {
            DataSourceId = Guid.NewGuid(),
            Query = "SELECT * FROM test"
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.ExecuteQueryAsync(request));
    }

    [Fact]
    public async Task ExecuteQueryAsync_ApiDataSource_QueriesApi()
    {
        // Arrange
        var dataSource = TestDataBuilder.CreateDataSource("Test API", "api", "https://api.example.com");
        Context.DataSources.Add(dataSource);
        await Context.SaveChangesAsync();

        var request = new ExecuteQueryRequest
        {
            DataSourceId = dataSource.Id,
            Query = "users",
            PageNumber = 1,
            PageSize = 10
        };

        var testData = new List<Dictionary<string, object?>>
        {
            new() { ["id"] = 1, ["name"] = "User 1" }
        };

        _apiDataSourceMock.Setup(a => a.QueryAsync("https://api.example.com/users", "GET", null, null, It.IsAny<CancellationToken>()))
            .Returns(CreateAsyncEnumerable(testData));

        // Act
        var result = await _service.ExecuteQueryAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Data.Should().HaveCount(1);
        result.Columns.Should().HaveCount(2);
        result.QueryExecutionTime.Should().NotBeNull();
    }

    [Fact]
    public async Task ExecuteQueryAsync_LimitsResults_ApiDataSource()
    {
        // Arrange
        var dataSource = TestDataBuilder.CreateDataSource("Test API", "api", "https://api.example.com");
        Context.DataSources.Add(dataSource);
        await Context.SaveChangesAsync();

        var request = new ExecuteQueryRequest
        {
            DataSourceId = dataSource.Id,
            Query = "items",
            PageNumber = 1,
            PageSize = 10
        };

        // 生成超过 1000 条的测试数据
        var testData = Enumerable.Range(1, 1500)
            .Select(i => new Dictionary<string, object?> { ["id"] = i })
            .ToList();

        _apiDataSourceMock.Setup(a => a.QueryAsync(It.IsAny<string>(), "GET", null, null, It.IsAny<CancellationToken>()))
            .Returns(CreateAsyncEnumerable(testData));

        // Act
        var result = await _service.ExecuteQueryAsync(request);

        // Assert - API 查询限制在 1000 条
        result.Data.Count.Should().Be(1000);
    }

    #endregion

    #region Helper Methods

    private ReportDefinition CreateApiReportDefinition()
    {
        var config = new Dictionary<string, object>
        {
            ["type"] = "api",
            ["apiUrl"] = "https://api.example.com/data",
            ["method"] = "GET"
        };

        return new ReportDefinition
        {
            Id = Guid.NewGuid(),
            Name = "API Test Report",
            DataSourceType = "api",
            DataSourceConfig = JsonSerializer.Serialize(config),
            QueryTemplate = "SELECT * FROM data",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Columns = new List<ReportColumn>
            {
                TestDataBuilder.CreateReportColumn("id", "ID", "number"),
                TestDataBuilder.CreateReportColumn("name", "Name", "string")
            }
        };
    }

    private async IAsyncEnumerable<T> CreateAsyncEnumerable<T>(List<T> items)
    {
        await Task.Yield(); // 避免同步警告
        foreach (var item in items)
        {
            yield return item;
        }
    }

    private async IAsyncEnumerable<Dictionary<string, object?>> CreateFailingAsyncEnumerable(Exception ex)
    {
        await Task.Yield();
        throw ex;
        yield break; // 永远不会到达
    }

    #endregion
}
