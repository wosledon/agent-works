using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using OneReport.Data.Entities;
using OneReport.Models.DTOs;
using OneReport.Models.Requests;
using OneReport.Services.Implementations;

namespace OneReport.Tests.Services;

public class DataSourceServiceTests : TestBase
{
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly Mock<ILogger<DataSourceService>> _loggerMock;
    private readonly DataSourceService _service;

    public DataSourceServiceTests()
    {
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _loggerMock = new Mock<ILogger<DataSourceService>>();
        _service = new DataSourceService(Context, _httpClientFactoryMock.Object, _loggerMock.Object);
    }

    #region GetByIdAsync

    [Fact]
    public async Task GetByIdAsync_ExistingDataSource_ReturnsDto()
    {
        // Arrange
        var dataSource = TestDataBuilder.CreateDataSource("Test DB", "mysql");
        Context.DataSources.Add(dataSource);
        await Context.SaveChangesAsync();

        // Act
        var result = await _service.GetByIdAsync(dataSource.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(dataSource.Id);
        result.Name.Should().Be("Test DB");
        result.Type.Should().Be("mysql");
        result.ConnectionString.Should().Contain("***");
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingDataSource_ReturnsNull()
    {
        // Act
        var result = await _service.GetByIdAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetListAsync

    [Fact]
    public async Task GetListAsync_NoSearch_ReturnsPagedList()
    {
        // Arrange
        var ds1 = TestDataBuilder.CreateDataSource("Alpha DB", "mysql");
        var ds2 = TestDataBuilder.CreateDataSource("Beta DB", "postgresql");
        var ds3 = TestDataBuilder.CreateDataSource("Gamma DB", "mysql");
        Context.DataSources.AddRange(ds1, ds2, ds3);
        await Context.SaveChangesAsync();

        // Act
        var result = await _service.GetListAsync(1, 10);

        // Assert
        result.Items.Should().HaveCount(3);
        result.TotalCount.Should().Be(3);
        result.TotalPages.Should().Be(1);
    }

    [Fact]
    public async Task GetListAsync_WithSearch_ReturnsFilteredList()
    {
        // Arrange
        var ds1 = TestDataBuilder.CreateDataSource("MySQL Production", "mysql");
        var ds2 = TestDataBuilder.CreateDataSource("PostgreSQL Analytics", "postgresql");
        Context.DataSources.AddRange(ds1, ds2);
        await Context.SaveChangesAsync();

        // Act
        var result = await _service.GetListAsync(1, 10, "mysql");

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items[0].Name.Should().Be("MySQL Production");
    }

    [Fact]
    public async Task GetListAsync_Pagination_ReturnsCorrectPage()
    {
        // Arrange
        for (int i = 1; i <= 5; i++)
        {
            Context.DataSources.Add(TestDataBuilder.CreateDataSource($"DB {i}", "mysql"));
        }
        await Context.SaveChangesAsync();

        // Act
        var result = await _service.GetListAsync(2, 2);

        // Assert
        result.Items.Should().HaveCount(2);
        result.PageNumber.Should().Be(2);
        result.TotalPages.Should().Be(3);
    }

    #endregion

    #region GetAllAsync

    [Fact]
    public async Task GetAllAsync_ReturnsOnlyActiveDataSources()
    {
        // Arrange
        var active = TestDataBuilder.CreateDataSource("Active DB", "mysql");
        var inactive = TestDataBuilder.CreateDataSource("Inactive DB", "mysql");
        inactive.IsActive = false;
        Context.DataSources.AddRange(active, inactive);
        await Context.SaveChangesAsync();

        // Act
        var result = await _service.GetAllAsync();

        // Assert
        result.Should().HaveCount(1);
        result[0].Name.Should().Be("Active DB");
    }

    [Fact]
    public async Task GetAllAsync_EmptyDatabase_ReturnsEmptyList()
    {
        // Act
        var result = await _service.GetAllAsync();

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region CreateAsync

    [Fact]
    public async Task CreateAsync_ValidDto_CreatesDataSource()
    {
        // Arrange - 模拟 API 连接测试成功
        var httpClient = new HttpClient(new MockHttpMessageHandler());
        _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var dto = new CreateDataSourceDto
        {
            Name = "New DataSource",
            Type = "api",
            ConnectionString = "https://api.example.com/data"
        };

        // Act
        var result = await _service.CreateAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("New DataSource");
        result.Type.Should().Be("api");
        
        // 验证数据库
        var saved = await Context.DataSources.FindAsync(result.Id);
        saved.Should().NotBeNull();
        saved!.Name.Should().Be("New DataSource");
    }

    [Fact]
    public async Task CreateAsync_ConnectionFails_ThrowsException()
    {
        // Arrange - 不设置 mock，让连接测试失败
        var dto = new CreateDataSourceDto
        {
            Name = "Invalid DataSource",
            Type = "mysql",
            ConnectionString = "invalid-connection-string"
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CreateAsync(dto));
    }

    #endregion

    #region UpdateAsync

    [Fact]
    public async Task UpdateAsync_ExistingDataSource_UpdatesSuccessfully()
    {
        // Arrange
        var httpClient = new HttpClient(new MockHttpMessageHandler());
        _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var dataSource = TestDataBuilder.CreateDataSource("Old Name", "api");
        Context.DataSources.Add(dataSource);
        await Context.SaveChangesAsync();

        var dto = new UpdateDataSourceDto
        {
            Name = "New Name",
            Type = "api",
            ConnectionString = "https://api.example.com/data",
            IsActive = true
        };

        // Act
        var result = await _service.UpdateAsync(dataSource.Id, dto);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("New Name");
        
        var updated = await Context.DataSources.FindAsync(dataSource.Id);
        updated!.Name.Should().Be("New Name");
        updated.UpdatedAt.Should().NotBeNull();
        updated.UpdatedAt.Should().BeAfter(dataSource.CreatedAt);
    }

    [Fact]
    public async Task UpdateAsync_NonExistingDataSource_ReturnsNull()
    {
        // Arrange
        var dto = new UpdateDataSourceDto
        {
            Name = "Test",
            Type = "mysql",
            ConnectionString = "test",
            IsActive = true
        };

        // Act
        var result = await _service.UpdateAsync(Guid.NewGuid(), dto);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateAsync_ConnectionStringChanged_ConnectionTestFails_ThrowsException()
    {
        // Arrange
        var dataSource = TestDataBuilder.CreateDataSource("Test", "mysql");
        Context.DataSources.Add(dataSource);
        await Context.SaveChangesAsync();

        var dto = new UpdateDataSourceDto
        {
            Name = "Test",
            Type = "mysql",
            ConnectionString = "invalid-connection-string",
            IsActive = true
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.UpdateAsync(dataSource.Id, dto));
    }

    #endregion

    #region DeleteAsync

    [Fact]
    public async Task DeleteAsync_ExistingDataSource_DeletesSuccessfully()
    {
        // Arrange
        var dataSource = TestDataBuilder.CreateDataSource();
        Context.DataSources.Add(dataSource);
        await Context.SaveChangesAsync();

        // Act
        var result = await _service.DeleteAsync(dataSource.Id);

        // Assert
        result.Should().BeTrue();
        var deleted = await Context.DataSources.FindAsync(dataSource.Id);
        deleted.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_NonExistingDataSource_ReturnsFalse()
    {
        // Act
        var result = await _service.DeleteAsync(Guid.NewGuid());

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region Connection String Masking

    [Theory]
    [InlineData("mysql", "Server=localhost;Uid=root;Pwd=secret123;", "Pwd=***")]
    [InlineData("postgresql", "Host=db;Password=secret123;", "Password=***")]
    [InlineData("api", "https://api.example.com/v1/data", "https://api.example.com/***")]
    public void ConnectionString_MaskedCorrectly(string type, string connectionString, string expectedMaskPart)
    {
        // Arrange - 通过创建 DTO 来验证掩码逻辑
        var dataSource = new DataSource
        {
            Id = Guid.NewGuid(),
            Name = "Test",
            Type = type,
            ConnectionString = connectionString,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        Context.DataSources.Add(dataSource);
        Context.SaveChanges();

        // Act
        var result = _service.GetByIdAsync(dataSource.Id).Result;

        // Assert
        result.Should().NotBeNull();
        result!.ConnectionString.Should().Contain(expectedMaskPart);
        result.ConnectionString.Should().NotContain("secret123");
    }

    #endregion
}

/// <summary>
/// 模拟 HTTP 消息处理器，用于测试 API 连接
/// </summary>
public class MockHttpMessageHandler : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK));
    }
}
