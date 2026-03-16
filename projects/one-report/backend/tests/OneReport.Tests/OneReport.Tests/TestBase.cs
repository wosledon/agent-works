using Microsoft.EntityFrameworkCore;
using OneReport.Data;
using OneReport.Data.Entities;

namespace OneReport.Tests;

/// <summary>
/// 测试基类，提供内存数据库上下文
/// </summary>
public abstract class TestBase : IDisposable
{
    protected AppDbContext Context { get; }

    protected TestBase()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        Context = new AppDbContext(options);
        Context.Database.EnsureCreated();
    }

    public void Dispose()
    {
        Context.Database.EnsureDeleted();
        Context.Dispose();
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// 测试数据构建器
/// </summary>
public static class TestDataBuilder
{
    public static DataSource CreateDataSource(
        string name = "Test DataSource",
        string type = "mysql",
        string connectionString = "Server=localhost;Database=test;Uid=root;Pwd=password;")
    {
        return new DataSource
        {
            Id = Guid.NewGuid(),
            Name = name,
            Type = type,
            ConnectionString = connectionString,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public static ReportDefinition CreateReportDefinition(
        string name = "Test Report",
        string dataSourceType = "sql")
    {
        return new ReportDefinition
        {
            Id = Guid.NewGuid(),
            Name = name,
            DataSourceType = dataSourceType,
            QueryTemplate = "SELECT * FROM test",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Columns = new List<ReportColumn>()
        };
    }

    public static ReportColumn CreateReportColumn(
        string fieldName = "test_field",
        string displayName = "Test Field",
        string dataType = "string")
    {
        return new ReportColumn
        {
            Id = Guid.NewGuid(),
            FieldName = fieldName,
            DisplayName = displayName,
            DataType = dataType,
            IsVisible = true,
            DisplayOrder = 0
        };
    }
}
