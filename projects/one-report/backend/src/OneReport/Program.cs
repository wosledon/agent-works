using Microsoft.EntityFrameworkCore;
using Npgsql;
using OneReport.Data;
using OneReport.Services.Implementations;
using OneReport.Services.Interfaces;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// 添加服务到容器
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "OneReport API", Version = "v1" });
});

// ========================================
// 数据库配置：PostgreSQL 或 SQLite 自动切换
// ========================================
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

if (!string.IsNullOrWhiteSpace(connectionString) 
    && !connectionString.Contains("Data Source=", StringComparison.OrdinalIgnoreCase))
{
    // 使用 PostgreSQL
    var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
    dataSourceBuilder.EnableDynamicJson();
    var dataSource = dataSourceBuilder.Build();

    builder.Services.AddDbContext<AppDbContext>(options =>
    {
        options.UseNpgsql(dataSource, npgsqlOptions =>
        {
            npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "public");
            npgsqlOptions.MaxBatchSize(100);
            npgsqlOptions.CommandTimeout(60);
        });
        options.UseSnakeCaseNamingConvention();
        
        if (builder.Environment.IsDevelopment())
        {
            options.EnableSensitiveDataLogging();
            options.EnableDetailedErrors();
        }
    });

    Console.WriteLine("[Database] Using PostgreSQL");
}
else
{
    // 使用 SQLite 作为降级方案
    var sqlitePath = connectionString?.Contains("Data Source=") == true
        ? connectionString.Replace("Data Source=", "").Trim()
        : builder.Configuration.GetValue("Database:SqlitePath", "data/one-report.db");
    
    var fullPath = Path.IsPathRooted(sqlitePath) 
        ? sqlitePath 
        : Path.Combine(builder.Environment.ContentRootPath, sqlitePath);
    
    Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
    
    builder.Services.AddDbContext<AppDbContext>(options =>
    {
        options.UseSqlite($"Data Source={fullPath}");
        options.UseSnakeCaseNamingConvention();
        
        if (builder.Environment.IsDevelopment())
        {
            options.EnableSensitiveDataLogging();
            options.EnableDetailedErrors();
        }
    });

    Console.WriteLine($"[Database] Using SQLite: {fullPath}");
}

// ========================================
// 缓存配置：Redis 或 MemoryCache 自动切换
// ========================================
var redisConnection = builder.Configuration.GetConnectionString("Redis");

if (!string.IsNullOrWhiteSpace(redisConnection))
{
    // 使用 Redis
    try
    {
        var redis = ConnectionMultiplexer.Connect(redisConnection);
        builder.Services.AddSingleton<IConnectionMultiplexer>(redis);
        builder.Services.AddSingleton<ICacheService, RedisCacheService>();
        Console.WriteLine("[Cache] Using Redis");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[Cache] Redis connection failed: {ex.Message}, falling back to MemoryCache");
        builder.Services.AddMemoryCache();
        builder.Services.AddSingleton<ICacheService, MemoryCacheService>();
    }
}
else
{
    // 使用 MemoryCache 作为降级方案
    builder.Services.AddMemoryCache();
    builder.Services.AddSingleton<ICacheService, MemoryCacheService>();
    Console.WriteLine("[Cache] Using MemoryCache (fallback mode)");
}

// 注册业务服务
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
        Console.WriteLine("[Database] Migration completed");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[Database] Migration failed: {ex.Message}");
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
