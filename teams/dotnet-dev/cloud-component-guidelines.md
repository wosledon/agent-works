# 云端组件本地开发规范

## 原则

**所有云端依赖必须提供本地/内存降级方案**，确保：
- 无云端服务时也能本地运行
- 单元测试无需外部依赖
- 新成员 5 分钟启动项目

---

## 组件对照表

| 云端组件 | 生产实现 | 本地/内存实现 | 切换方式 |
|---------|---------|--------------|---------|
| **数据库** | PostgreSQL | SQLite | 连接字符串检测 |
| **缓存** | Redis | 内存 Dictionary | 配置缺失时自动降级 |
| **消息队列** | RabbitMQ/Kafka | C# Channel | 连接失败时自动降级 |
| **对象存储** | S3/OSS | 本地文件系统 | 配置区分 |
| **搜索引擎** | Elasticsearch | 内存过滤 | 配置缺失时跳过 |

---

## 实现要求

### 1. 抽象接口

所有云端组件必须通过抽象接口访问，禁止直接依赖具体实现：

```csharp
// ✅ 正确：通过接口
public interface ICacheService {
    Task<T> GetAsync<T>(string key);
    Task SetAsync<T>(string key, T value, TimeSpan? expiry = null);
}

// ❌ 错误：直接依赖 Redis
public class MyService {
    private readonly IDatabase _redis; // 直接依赖
}
```

### 2. 依赖注入配置

```csharp
// Program.cs
if (configuration.GetConnectionString("Redis") != null) {
    services.AddSingleton<ICacheService, RedisCacheService>();
} else {
    services.AddSingleton<ICacheService, MemoryCacheService>();
    logger.LogWarning("Redis 未配置，使用内存缓存（仅本地开发）");
}
```

### 3. 配置文件示例

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=mydb;...",
    "Redis": "localhost:6379",
    "RabbitMQ": "amqp://localhost:5672"
  },
  "LocalDev": {
    "UseSQLite": true,
    "UseMemoryCache": true,
    "UseChannelMQ": true
  }
}
```

### 4. 本地实现示例

#### 内存缓存
```csharp
public class MemoryCacheService : ICacheService {
    private readonly ConcurrentDictionary<string, object> _cache = new();
    
    public Task<T> GetAsync<T>(string key) {
        _cache.TryGetValue(key, out var value);
        return Task.FromResult((T)value);
    }
    // ...
}
```

#### Channel-based MQ
```csharp
public class ChannelMessageQueue : IMessageQueue {
    private readonly Channel<Message> _channel = Channel.CreateUnbounded<Message>();
    
    public Task PublishAsync(Message message) {
        _channel.Writer.TryWrite(message);
        return Task.CompletedTask;
    }
    // ...
}
```

---

## 测试要求

- 单元测试：必须使用内存实现，禁止连接外部服务
- 集成测试：可选连接真实服务，但需标记 `[SkipIfNoDocker]`

```csharp
[Fact]
public async Task Should_Create_Post() {
    // 使用内存数据库，无需 PostgreSQL
    var context = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
        .UseSqlite("DataSource=:memory:")
        .Options);
    // ...
}
```

---

## 开发启动检查清单

- [ ] 无 Docker 也能运行 `dotnet run`
- [ ] 无环境变量也能通过基础测试
- [ ] 新成员 5 分钟完成首次启动
- [ ] 配置文件包含本地开发默认值
