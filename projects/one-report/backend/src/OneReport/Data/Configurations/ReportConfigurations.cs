using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OneReport.Data.Entities;

namespace OneReport.Data.Configurations;

public class ReportDefinitionConfiguration : IEntityTypeConfiguration<ReportDefinition>
{
    public void Configure(EntityTypeBuilder<ReportDefinition> builder)
    {
        builder.ToTable("report_definitions");
        
        builder.HasKey(e => e.Id);
        
        builder.Property(e => e.Name)
            .HasMaxLength(200)
            .IsRequired();
        
        builder.Property(e => e.Description)
            .HasMaxLength(1000);
        
        builder.Property(e => e.DataSourceType)
            .HasMaxLength(50)
            .IsRequired();
        
        builder.Property(e => e.DataSourceConfig)
            .HasColumnType("jsonb");
        
        builder.Property(e => e.QueryTemplate)
            .HasColumnType("text");
        
        builder.Property(e => e.IsActive)
            .HasDefaultValue(true);
        
        builder.HasMany(e => e.Columns)
            .WithOne(c => c.ReportDefinition)
            .HasForeignKey(c => c.ReportDefinitionId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasIndex(e => e.Name);
        builder.HasIndex(e => e.IsActive);
        builder.HasIndex(e => e.CreatedAt);
    }
}

public class ReportColumnConfiguration : IEntityTypeConfiguration<ReportColumn>
{
    public void Configure(EntityTypeBuilder<ReportColumn> builder)
    {
        builder.ToTable("report_columns");
        
        builder.HasKey(e => e.Id);
        
        builder.Property(e => e.FieldName)
            .HasMaxLength(200)
            .IsRequired();
        
        builder.Property(e => e.DisplayName)
            .HasMaxLength(200)
            .IsRequired();
        
        builder.Property(e => e.DataType)
            .HasMaxLength(50)
            .IsRequired();
        
        builder.Property(e => e.Format)
            .HasMaxLength(100);
        
        builder.Property(e => e.Aggregation)
            .HasMaxLength(50);
        
        builder.HasIndex(e => e.ReportDefinitionId);
        builder.HasIndex(e => e.DisplayOrder);
    }
}

public class ReportExportHistoryConfiguration : IEntityTypeConfiguration<ReportExportHistory>
{
    public void Configure(EntityTypeBuilder<ReportExportHistory> builder)
    {
        builder.ToTable("report_export_histories");
        
        builder.HasKey(e => e.Id);
        
        builder.Property(e => e.ExportFormat)
            .HasMaxLength(50)
            .IsRequired();
        
        builder.Property(e => e.Status)
            .HasMaxLength(50)
            .IsRequired();
        
        builder.Property(e => e.FilePath)
            .HasMaxLength(500);
        
        builder.Property(e => e.ErrorMessage)
            .HasColumnType("text");
        
        builder.Property(e => e.Parameters)
            .HasColumnType("jsonb");
        
        builder.HasIndex(e => e.ReportDefinitionId);
        builder.HasIndex(e => e.Status);
        builder.HasIndex(e => e.CreatedAt);
    }
}

public class DataSourceConfiguration : IEntityTypeConfiguration<DataSource>
{
    public void Configure(EntityTypeBuilder<DataSource> builder)
    {
        builder.ToTable("data_sources");
        
        builder.HasKey(e => e.Id);
        
        builder.Property(e => e.Name)
            .HasMaxLength(200)
            .IsRequired();
        
        builder.Property(e => e.Type)
            .HasMaxLength(50)
            .IsRequired();
        
        builder.Property(e => e.ConnectionString)
            .IsRequired();
        
        builder.HasIndex(e => e.Name)
            .IsUnique();
        builder.HasIndex(e => e.IsActive);
    }
}

public class ReportExecutionLogConfiguration : IEntityTypeConfiguration<ReportExecutionLog>
{
    public void Configure(EntityTypeBuilder<ReportExecutionLog> builder)
    {
        builder.ToTable("report_execution_logs");
        
        builder.HasKey(e => e.Id);
        
        builder.Property(e => e.OperationType)
            .HasMaxLength(50)
            .IsRequired();
        
        builder.Property(e => e.Status)
            .HasMaxLength(50)
            .IsRequired();
        
        builder.Property(e => e.Parameters)
            .HasColumnType("jsonb");
        
        builder.Property(e => e.ErrorMessage)
            .HasColumnType("text");
        
        builder.Property(e => e.ExportFormat)
            .HasMaxLength(50);
        
        builder.Property(e => e.ClientIp)
            .HasMaxLength(50);
        
        builder.HasIndex(e => e.ReportDefinitionId);
        builder.HasIndex(e => e.Status);
        builder.HasIndex(e => e.StartedAt);
        builder.HasIndex(e => e.ExecutedBy);
        
        // 复合索引：报表ID + 执行时间
        builder.HasIndex(e => new { e.ReportDefinitionId, e.StartedAt });
    }
}

public class QueryResultCacheConfiguration : IEntityTypeConfiguration<QueryResultCache>
{
    public void Configure(EntityTypeBuilder<QueryResultCache> builder)
    {
        builder.ToTable("query_result_caches");
        
        builder.HasKey(e => e.Id);
        
        builder.Property(e => e.CacheKey)
            .HasMaxLength(500)
            .IsRequired();
        
        builder.Property(e => e.Parameters)
            .HasColumnType("jsonb");
        
        builder.Property(e => e.Data)
            .HasColumnType("jsonb")
            .IsRequired();
        
        builder.HasIndex(e => e.CacheKey)
            .IsUnique();
        builder.HasIndex(e => e.ReportDefinitionId);
        builder.HasIndex(e => e.ExpiresAt);
        
        // 用于清理过期缓存
        builder.HasIndex(e => new { e.ExpiresAt, e.HitCount });
    }
}

public class UserPermissionConfiguration : IEntityTypeConfiguration<UserPermission>
{
    public void Configure(EntityTypeBuilder<UserPermission> builder)
    {
        builder.ToTable("user_permissions");
        
        builder.HasKey(e => e.Id);
        
        builder.Property(e => e.ResourceType)
            .HasMaxLength(50)
            .IsRequired();
        
        builder.Property(e => e.Permission)
            .HasMaxLength(50)
            .IsRequired();
        
        builder.HasIndex(e => e.UserId);
        builder.HasIndex(e => new { e.UserId, e.ResourceType, e.ResourceId });
    }
}

public class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> builder)
    {
        builder.ToTable("user_roles");
        
        builder.HasKey(e => e.Id);
        
        builder.Property(e => e.Role)
            .HasMaxLength(50)
            .IsRequired();
        
        builder.HasIndex(e => e.UserId)
            .IsUnique();
    }
}

public class ChartDefinitionConfiguration : IEntityTypeConfiguration<ChartDefinition>
{
    public void Configure(EntityTypeBuilder<ChartDefinition> builder)
    {
        builder.ToTable("chart_definitions");
        
        builder.HasKey(e => e.Id);
        
        builder.Property(e => e.Name)
            .HasMaxLength(200)
            .IsRequired();
        
        builder.Property(e => e.Description)
            .HasMaxLength(1000);
        
        builder.Property(e => e.ChartType)
            .HasMaxLength(50)
            .IsRequired();
        
        builder.Property(e => e.Query)
            .HasColumnType("text")
            .IsRequired();
        
        builder.Property(e => e.Config)
            .HasColumnType("jsonb");
        
        builder.Property(e => e.IsActive)
            .HasDefaultValue(true);
        
        builder.HasOne(e => e.DataSource)
            .WithMany()
            .HasForeignKey(e => e.DataSourceId)
            .OnDelete(DeleteBehavior.Restrict);
        
        builder.HasIndex(e => e.Name);
        builder.HasIndex(e => e.DataSourceId);
        builder.HasIndex(e => e.IsActive);
        builder.HasIndex(e => e.CreatedAt);
    }
}
