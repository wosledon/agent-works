namespace OneReport.Models.DTOs;

/// <summary>
/// 报表定义DTO
/// </summary>
public class ReportDefinitionDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string DataSourceType { get; set; } = string.Empty;
    public string? DataSourceConfig { get; set; }
    public string? QueryTemplate { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public List<ReportColumnDto> Columns { get; set; } = new();
}

/// <summary>
/// 创建报表定义请求
/// </summary>
public class CreateReportDefinitionDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string DataSourceType { get; set; } = string.Empty;
    public string? DataSourceConfig { get; set; }
    public string? QueryTemplate { get; set; }
    public List<CreateReportColumnDto> Columns { get; set; } = new();
}

/// <summary>
/// 更新报表定义请求
/// </summary>
public class UpdateReportDefinitionDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string DataSourceType { get; set; } = string.Empty;
    public string? DataSourceConfig { get; set; }
    public string? QueryTemplate { get; set; }
    public bool IsActive { get; set; }
    public List<UpdateReportColumnDto> Columns { get; set; } = new();
}

/// <summary>
/// 报表列DTO
/// </summary>
public class ReportColumnDto
{
    public Guid Id { get; set; }
    public string FieldName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public bool IsVisible { get; set; }
    public string? Format { get; set; }
    public int? Width { get; set; }
    public string? Aggregation { get; set; }
}

/// <summary>
/// 创建报表列请求
/// </summary>
public class CreateReportColumnDto
{
    public string FieldName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string DataType { get; set; } = "string";
    public int DisplayOrder { get; set; }
    public bool IsVisible { get; set; } = true;
    public string? Format { get; set; }
    public int? Width { get; set; }
    public string? Aggregation { get; set; }
}

/// <summary>
/// 更新报表列请求
/// </summary>
public class UpdateReportColumnDto
{
    public Guid? Id { get; set; } // 新列没有ID
    public string FieldName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string DataType { get; set; } = "string";
    public int DisplayOrder { get; set; }
    public bool IsVisible { get; set; } = true;
    public string? Format { get; set; }
    public int? Width { get; set; }
    public string? Aggregation { get; set; }
}

/// <summary>
/// 数据源DTO
/// </summary>
public class DataSourceDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string ConnectionString { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// 创建数据源请求
/// </summary>
public class CreateDataSourceDto
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string ConnectionString { get; set; } = string.Empty;
}

/// <summary>
/// 更新数据源请求
/// </summary>
public class UpdateDataSourceDto
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string ConnectionString { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}
