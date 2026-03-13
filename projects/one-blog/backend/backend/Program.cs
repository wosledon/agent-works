using DotnetBlog.Data;
using DotnetBlog.Endpoints;
using DotnetBlog.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using StackExchange.Redis;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddEndpointsApiExplorer();

// Configure Swagger with JWT support
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "DotnetBlog API",
        Version = "v1",
        Description = "A modern blog API with JWT authentication"
    });

    // Add JWT Authentication to Swagger
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Bearer {token}\""
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Add Database Context with auto-fallback to SQLite
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<BlogDbContext>(options =>
{
    if (string.IsNullOrEmpty(connectionString) || 
        connectionString.Contains(":memory:") || 
        connectionString.EndsWith(".db", StringComparison.OrdinalIgnoreCase) ||
        connectionString.EndsWith(".sqlite", StringComparison.OrdinalIgnoreCase))
    {
        // Use SQLite for in-memory or file-based databases
        var sqliteConnection = string.IsNullOrEmpty(connectionString) ? "DataSource=blog.db" : connectionString;
        options.UseSqlite(sqliteConnection);
        builder.Configuration["DatabaseProvider"] = "SQLite";
    }
    else
    {
        // Use PostgreSQL for production connections
        options.UseNpgsql(connectionString, npgsqlOptions =>
        {
            npgsqlOptions.MigrationsAssembly("DotnetBlog");
        });
        builder.Configuration["DatabaseProvider"] = "PostgreSQL";
    }
});

// Add JWT Authentication
var jwtSecretKey = builder.Configuration["Jwt:SecretKey"];
if (string.IsNullOrEmpty(jwtSecretKey))
{
    // Generate a secure key for development if not configured
    jwtSecretKey = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
    builder.Configuration["Jwt:SecretKey"] = jwtSecretKey;
}

var jwtRefreshSecretKey = builder.Configuration["Jwt:RefreshSecretKey"];
if (string.IsNullOrEmpty(jwtRefreshSecretKey))
{
    jwtRefreshSecretKey = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
    builder.Configuration["Jwt:RefreshSecretKey"] = jwtRefreshSecretKey;
}

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "DotnetBlog",
        ValidAudience = builder.Configuration["Jwt:Audience"] ?? "DotnetBlogClient",
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretKey)),
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

// Add MemoryCache (always available for fallback)
builder.Services.AddMemoryCache();

// Configure Cache Service with Redis or Memory fallback
var redisConnectionString = builder.Configuration.GetConnectionString("Redis");
if (!string.IsNullOrEmpty(redisConnectionString))
{
    try
    {
        var redis = ConnectionMultiplexer.Connect(redisConnectionString);
        builder.Services.AddSingleton<IConnectionMultiplexer>(redis);
        builder.Services.AddSingleton<ICacheService, RedisCacheService>();
        builder.Configuration["CacheProvider"] = "Redis";
    }
    catch (Exception ex)
    {
        builder.Configuration["CacheProvider"] = "Memory (Redis failed)";
        builder.Services.AddSingleton<ICacheService, MemoryCacheService>();
    }
}
else
{
    builder.Configuration["CacheProvider"] = "Memory";
    builder.Services.AddSingleton<ICacheService, MemoryCacheService>();
}

// Register custom services
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IAuthService, IAuthService>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Enable authentication and authorization
app.UseAuthentication();
app.UseAuthorization();

// Map endpoints
app.MapGet("/", () => "DotnetBlog API is running!");

app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

app.MapAuthEndpoints();
app.MapPostEndpoints();  // Keep existing endpoints for backward compatibility
app.MapCategoryEndpoints();
app.MapArticleEndpoints();  // New comprehensive article endpoints

app.Run();
