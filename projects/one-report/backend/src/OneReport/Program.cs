using Microsoft.EntityFrameworkCore;
using OneReport.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 数据库配置：根据连接字符串自动选择 PostgreSQL 或 SQLite
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var useSqlite = string.IsNullOrWhiteSpace(connectionString);

if (useSqlite)
{
    // 使用 SQLite 作为降级方案
    var sqlitePath = builder.Configuration.GetValue("Database:SqlitePath", "data/one-report.db");
    var fullPath = Path.Combine(builder.Environment.ContentRootPath, sqlitePath);
    Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
    
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseSqlite($"Data Source={fullPath}")
               .UseSnakeCaseNamingConvention());
    
    builder.Services.AddDbContext<ReportDbContext>(options =>
        options.UseSqlite($"Data Source={fullPath}")
               .UseSnakeCaseNamingConvention());
    
    builder.Services.AddSingleton(new DatabaseOptions { Provider = "SQLite", ConnectionString = sqlitePath });
}
else
{
    // 使用 PostgreSQL
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseNpgsql(connectionString)
               .UseSnakeCaseNamingConvention());
    
    builder.Services.AddDbContext<ReportDbContext>(options =>
        options.UseNpgsql(connectionString)
               .UseSnakeCaseNamingConvention());
    
    builder.Services.AddSingleton(new DatabaseOptions { Provider = "PostgreSQL", ConnectionString = connectionString });
}

var app = builder.Build();

// 自动迁移数据库
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    dbContext.Database.Migrate();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast =  Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast")
.WithOpenApi();

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

/// <summary>
/// 数据库配置选项
/// </summary>
public class DatabaseOptions
{
    public string Provider { get; set; } = "SQLite";
    public string ConnectionString { get; set; } = string.Empty;
}
