# One-Blog 降级支持文档

本项目支持云端组件的本地降级，无需 PostgreSQL 和 Redis 即可运行。

## 数据库降级

### 自动切换逻辑

Program.cs 根据连接字符串自动选择数据库提供程序：

| 连接字符串特征 | 选择的数据库 |
|--------------|-------------|
| 为空 | SQLite (文件模式: blog.db) |
| 包含 `:memory:` | SQLite (内存模式) |
| 以 `.db` 或 `.sqlite` 结尾 | SQLite (文件模式) |
| 其他 | PostgreSQL |

### 使用方式

**1. 内存模式（快速测试）**
```bash
# 使用提供的脚本
./scripts/start-memory.sh

# 或手动设置
export ConnectionStrings__DefaultConnection=":memory:"
dotnet run
```

**2. 文件模式（本地开发）**
```bash
# 初始化数据库
./scripts/init-sqlite.sh

# 运行应用
export ConnectionStrings__DefaultConnection="DataSource=data/blog.db"
dotnet run
```

**3. PostgreSQL 模式（生产环境）**
```bash
export ConnectionStrings__DefaultConnection="Host=localhost;Database=blog;Username=postgres;Password=secret"
dotnet run
```

## 缓存降级

### 自动切换逻辑

Program.cs 根据 Redis 连接配置自动选择缓存提供程序：

| Redis 连接字符串 | 选择的缓存 |
|----------------|-----------|
| 为空 | MemoryCache |
| 连接失败 | MemoryCache (自动降级) |
| 连接成功 | Redis |

### 使用方式

**1. 本地模式（无需 Redis）**
```bash
# 不设置或清空 Redis 连接字符串
export ConnectionStrings__Redis=""
dotnet run
```

**2. Redis 模式（生产环境）**
```bash
export ConnectionStrings__Redis="localhost:6379"
dotnet run
```

## 健康检查

访问 `/health` 端点可查看当前使用的提供程序：

```json
{
  "status": "healthy",
  "timestamp": "2026-03-14T04:55:00Z",
  "databaseProvider": "SQLite",
  "cacheProvider": "Memory"
}
```

## 接口说明

### ICacheService

统一的缓存服务接口：

```csharp
public interface ICacheService
{
    Task<T?> GetAsync<T>(string key);
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null);
    Task RemoveAsync(string key);
    Task<bool> ExistsAsync(string key);
}
```

### 实现类

- **MemoryCacheService**: 使用 `IMemoryCache`，适用于本地开发
- **RedisCacheService**: 使用 Redis，适用于生产环境

## 配置示例

### appsettings.Development.json（本地开发）

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "DataSource=data/blog.db",
    "Redis": ""
  }
}
```

### appsettings.Production.json（生产环境）

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=prod-db;Database=blog;Username=...",
    "Redis": "prod-redis:6379"
  }
}
```

## 注意事项

1. **SQLite 限制**: 某些高级 PostgreSQL 特性在 SQLite 中可能不支持
2. **内存模式**: 数据不会持久化，应用重启后数据丢失
3. **Redis 故障**: Redis 连接失败会自动降级到 MemoryCache，不会中断服务
