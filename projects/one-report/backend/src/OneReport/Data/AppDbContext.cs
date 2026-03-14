using Microsoft.EntityFrameworkCore;
using OneReport.Data.Configurations;
using OneReport.Data.Entities;

namespace OneReport.Data;

/// <summary>
/// 应用数据库上下文
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<ReportDefinition> ReportDefinitions { get; set; }
    public DbSet<ReportColumn> ReportColumns { get; set; }
    public DbSet<ReportExportHistory> ReportExportHistories { get; set; }
    public DbSet<DataSource> DataSources { get; set; }
    public DbSet<ReportExecutionLog> ReportExecutionLogs { get; set; }
    public DbSet<QueryResultCache> QueryResultCaches { get; set; }
    public DbSet<UserPermission> UserPermissions { get; set; }
    public DbSet<UserRole> UserRoles { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // 应用配置
        modelBuilder.ApplyConfiguration(new ReportDefinitionConfiguration());
        modelBuilder.ApplyConfiguration(new ReportColumnConfiguration());
        modelBuilder.ApplyConfiguration(new ReportExportHistoryConfiguration());
        modelBuilder.ApplyConfiguration(new DataSourceConfiguration());
        modelBuilder.ApplyConfiguration(new ReportExecutionLogConfiguration());
        modelBuilder.ApplyConfiguration(new QueryResultCacheConfiguration());
        modelBuilder.ApplyConfiguration(new UserPermissionConfiguration());
        modelBuilder.ApplyConfiguration(new UserRoleConfiguration());
    }
}

/// <summary>
/// 报表数据上下文 - 用于执行动态查询
/// 使用无跟踪查询以优化性能
/// </summary>
public class ReportDbContext : DbContext
{
    public ReportDbContext(DbContextOptions<ReportDbContext> options) : base(options)
    {
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
        
        // 为报表查询优化：禁用跟踪和启用分块查询
        optionsBuilder.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
    }
}
