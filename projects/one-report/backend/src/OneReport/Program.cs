using Microsoft.EntityFrameworkCore;
using Npgsql;
using OneReport.Data;
using OneReport.Services.Implementations;
using OneReport.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// 添加服务到容器
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "OneReport API", Version = "v1" });
    c.EnableAnnotations();
});

// 数据库配置 - PostgreSQL
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

// 配置 Npgsql 数据源以支持 JSONB
var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
dataSourceBuilder.EnableDynamicJson();
var dataSource = dataSourceBuilder.Build();

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseNpgsql(dataSource, npgsqlOptions =>
    {
        npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "public");
        // 启用批量操作优化
        npgsqlOptions.MaxBatchSize(100);
        npgsqlOptions.CommandTimeout(60);
    });
    
    // 使用 snake_case 命名约定
    options.UseSnakeCaseNamingConvention();
    
    // 开发环境启用敏感日志
    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
        options.EnableDetailedErrors();
    }
});

// 注册服务
builder.Services.AddScoped<IReportDefinitionService, ReportDefinitionService>();
builder.Services.AddScoped<IReportDataService, ReportDataService>();
builder.Services.AddScoped<IReportExportService, ReportExportService>();
builder.Services.AddScoped<IDataSourceService, DataSourceService>();

// 配置 CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// 配置响应压缩以支持流式导出
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
});

var app = builder.Build();

// 自动迁移数据库
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    try
    {
        db.Database.Migrate();
        Console.WriteLine("数据库迁移完成");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"数据库迁移失败: {ex.Message}");
    }
}

// 配置 HTTP 请求管道
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "OneReport API V1");
    });
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseResponseCompression();

// 确保导出目录存在
var webRootPath = app.Environment.WebRootPath ?? Path.Combine(app.Environment.ContentRootPath, "wwwroot");
Directory.CreateDirectory(Path.Combine(webRootPath, "exports"));

app.UseAuthorization();
app.MapControllers();

app.Run();
