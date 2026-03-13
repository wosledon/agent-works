# One-Report 架构设计文档

## 1. 项目概述

One-Report 是一个低代码报表工具，专注于高性能、低资源消耗的报表设计与导出。

### 1.1 核心特性
- 可视化报表设计器（React）
- 多数据源支持（SQL、API、混合）
- 流式导出（PDF/Excel）
- 实时数据预览
- 定时任务调度

### 1.2 性能目标
| 指标 | 目标值 |
|------|--------|
| 导出100万行数据 | 内存 < 500MB |
| 并发导出 | 支持10+并行 |
| 低配服务器 | 2核4GB可运行 |
| 报表渲染 | < 2秒（复杂报表） |

---

## 2. 技术架构

### 2.1 整体架构

```
┌─────────────────────────────────────────────────────────────┐
│                        Frontend (React)                      │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────────────┐  │
│  │ Report      │  │ Data Source │  │ Export Progress     │  │
│  │ Designer    │  │ Config      │  │ Monitor             │  │
│  └─────────────┘  └─────────────┘  └─────────────────────┘  │
└───────────────────────────┬─────────────────────────────────┘
                            │ HTTP/SignalR
┌───────────────────────────▼─────────────────────────────────┐
│                     API Gateway                              │
│              (Rate Limit / Auth / Routing)                   │
└───────────────────────────┬─────────────────────────────────┘
                            │
┌───────────────────────────▼─────────────────────────────────┐
│                    Backend (.NET 8)                          │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────────────┐  │
│  │ Report API  │  │ Export API  │  │ Data Query API      │  │
│  └─────────────┘  └─────────────┘  └─────────────────────┘  │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────────────┐  │
│  │ Auth API    │  │ Scheduler   │  │ File Storage        │  │
│  └─────────────┘  └─────────────┘  └─────────────────────┘  │
└───────────────────────────┬─────────────────────────────────┘
                            │
        ┌───────────────────┼───────────────────┐
        │                   │                   │
┌───────▼───────┐  ┌───────▼───────┐  ┌───────▼───────┐
│   PostgreSQL   │  │  Redis        │  │  S3/MinIO     │
│  (主数据库)     │  │ (缓存/队列)    │  │  (文件存储)    │
└───────────────┘  └───────────────┘  └───────────────┘
```

### 2.2 分层架构

| 层级 | 职责 | 技术选型 |
|------|------|----------|
| 表现层 | 可视化设计、交互 | React 18 + TypeScript |
| API层 | 请求路由、认证授权 | ASP.NET Core |
| 业务层 | 报表逻辑、导出引擎 | .NET 8 + 领域服务 |
| 数据层 | 数据访问、查询优化 | EF Core + Dapper |
| 基础设施 | 缓存、队列、存储 | Redis + RabbitMQ + S3 |

---

## 3. 性能优化方案

### 3.1 流式导出架构

```
┌─────────────────────────────────────────────────────────────┐
│                     Streaming Export Flow                    │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│   Data Source ──► Stream Reader ──► Transform ──► Writer     │
│        │                                                        │
│        │    [Chunk Size: 1000 rows]                           │
│        ▼                                                        │
│   ┌─────────────────────────────────────────────────────┐     │
│   │  IAsyncEnumerable<T> Pipeline                       │     │
│   │                                                      │     │
│   │  while (await reader.ReadAsync()) {                 │     │
│   │      yield return Transform(row);                   │     │
│   │      if (count % 1000 == 0) Flush();                │     │
│   │  }                                                  │     │
│   └─────────────────────────────────────────────────────┘     │
│        │                                                        │
│        ▼                                                        │
│   PDF: iText7 (流式写入)                                       │
│   Excel: OpenXml SDK (SAX模式) / MiniExcel                     │
│   CSV: StreamWriter (直接写入)                                 │
│                                                              │
└─────────────────────────────────────────────────────────────┘
```

### 3.2 内存控制策略

#### 3.2.1 分页查询
```csharp
// 流式数据读取 - 避免加载全部数据到内存
public async IAsyncEnumerable<DataRow> QueryStreamAsync(
    string sql, 
    [EnumeratorCancellation] CancellationToken ct = default)
{
    await using var connection = await _dataSource.OpenConnectionAsync();
    await using var command = new NpgsqlCommand(sql, connection);
    await using var reader = await command.ExecuteReaderAsync(ct);
    
    while (await reader.ReadAsync(ct))
    {
        yield return MapToRow(reader); // 逐行 yield，内存只保留一行
    }
}
```

#### 3.2.2 对象池复用
```csharp
// 使用 ArrayPool 减少 GC 压力
public class DataBufferPool
{
    private readonly ArrayPool<object[]> _pool = ArrayPool<object[]>.Shared;
    
    public object[] Rent(int size) => _pool.Rent(size);
    public void Return(object[] buffer) => _pool.Return(buffer, clearArray: true);
}
```

#### 3.2.3 分块处理
```csharp
// 大批量数据分块处理
public async Task ProcessInChunksAsync<T>(
    IAsyncEnumerable<T> source, 
    int chunkSize,
    Func<IReadOnlyList<T>, Task> processor)
{
    var chunk = new List<T>(chunkSize);
    
    await foreach (var item in source)
    {
        chunk.Add(item);
        
        if (chunk.Count >= chunkSize)
        {
            await processor(chunk);
            chunk.Clear(); // 复用列表，避免重新分配
        }
    }
    
    if (chunk.Count > 0)
    {
        await processor(chunk);
    }
}
```

### 3.3 导出引擎实现

#### 3.3.1 Excel 流式导出（使用 MiniExcel）
```csharp
public async Task ExportExcelStreamingAsync(
    string reportId, 
    Stream outputStream,
    CancellationToken ct)
{
    // MiniExcel 使用 SAX 模式，内存占用极低
    var dataStream = _queryService.GetDataStream(reportId);
    
    await MiniExcel.SaveAsAsync(
        outputStream, 
        dataStream, 
        excelType: ExcelType.XLSX,
        cancellationToken: ct);
}
```

#### 3.3.2 PDF 流式导出（使用 iText7）
```csharp
public async Task ExportPdfStreamingAsync(
    ReportTemplate template,
    Stream outputStream,
    CancellationToken ct)
{
    using var writer = new PdfWriter(outputStream);
    using var pdf = new PdfDocument(writer);
    var document = new Document(pdf);
    
    // 配置内存优化
    pdf.SetFlushUnusedObjects(true);
    writer.SetCloseStream(false);
    
    await foreach (var row in _queryService.QueryStreamAsync(template.Query, ct))
    {
        // 逐行写入 PDF
        AddRowToDocument(document, row, template);
        
        // 每 1000 行 flush 一次
        if (++rowCount % 1000 == 0)
        {
            pdf.FlushUnusedObjects();
        }
    }
    
    document.Close();
}
```

#### 3.3.3 CSV 流式导出
```csharp
public async Task ExportCsvStreamingAsync(
    string reportId,
    Stream outputStream,
    CancellationToken ct)
{
    var encoding = new UTF8Encoding(false); // 无 BOM
    await using var writer = new StreamWriter(outputStream, encoding, bufferSize: 65536);
    
    // 写入表头
    await writer.WriteLineAsync(GetCsvHeader());
    
    await foreach (var row in _queryService.QueryStreamAsync(reportId, ct))
    {
        await writer.WriteLineAsync(FormatCsvRow(row));
    }
}
```

### 3.4 多数据源查询优化

#### 3.4.1 连接池管理
```csharp
// 配置连接池以支持高并发
"ConnectionStrings": {
    "Default": "Host=localhost;Database=one_report;...;
                Maximum Pool Size=100;
                Connection Idle Lifetime=300;
                Connection Pruning Interval=10"
}
```

#### 3.4.2 查询超时与取消
```csharp
public class QueryOptions
{
    public int CommandTimeout { get; set; } = 300; // 5分钟
    public CancellationToken CancellationToken { get; set; }
    public int MaxRows { get; set; } = 1_000_000; // 默认最大100万行
}
```

#### 3.4.3 缓存策略
```csharp
// 热点数据缓存（报表配置、元数据）
public class ReportCacheService
{
    private readonly IDistributedCache _cache;
    
    public async Task<ReportTemplate> GetTemplateAsync(string reportId)
    {
        var cacheKey = $"report:template:{reportId}";
        var cached = await _cache.GetStringAsync(cacheKey);
        
        if (cached != null)
            return JsonSerializer.Deserialize<ReportTemplate>(cached);
        
        var template = await _dbContext.ReportTemplates
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == reportId);
        
        if (template != null)
        {
            await _cache.SetStringAsync(cacheKey, 
                JsonSerializer.Serialize(template),
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
                });
        }
        
        return template;
    }
}
```

---

## 4. 数据库设计要点

### 4.1 分区表设计（大数据量报表历史）
```sql
-- 报表结果历史表按月分区
CREATE TABLE report_results (
    id BIGSERIAL,
    report_id UUID NOT NULL,
    created_at TIMESTAMP NOT NULL,
    file_path VARCHAR(500),
    file_size BIGINT,
    status VARCHAR(20)
) PARTITION BY RANGE (created_at);

-- 创建月度分区
CREATE TABLE report_results_2024_01 PARTITION OF report_results
    FOR VALUES FROM ('2024-01-01') TO ('2024-02-01');
```

### 4.2 索引策略
```sql
-- 复合索引优化报表查询
CREATE INDEX idx_reports_user_status ON reports(user_id, status, created_at DESC);

-- 部分索引（只索引活跃报表）
CREATE INDEX idx_active_reports ON reports(created_at) 
    WHERE status = 'active';

-- GIN 索引用于 JSONB 查询
CREATE INDEX idx_report_config_gin ON reports 
    USING GIN (config jsonb_path_ops);
```

---

## 5. 部署架构

### 5.1 低配服务器优化

```yaml
# docker-compose.yml 低配版本
services:
  api:
    image: one-report-api
    deploy:
      resources:
        limits:
          cpus: '1.5'
          memory: 1.5G
    environment:
      - DOTNET_GCHeapCount=2      # 限制 GC 线程数
      - DOTNET_GCConserveMemory=1  # 内存节省模式
      - DOTNET_ThreadPool_UnfairSemaphoreSpinLimit=0
      
  postgres:
    image: postgres:16-alpine
    command: >
      postgres
      -c shared_buffers=256MB
      -c effective_cache_size=768MB
      -c work_mem=8MB
      -c maintenance_work_mem=64MB
      -c max_connections=50
```

### 5.2 水平扩展
```
┌─────────────────────────────────────────┐
│            Load Balancer (Nginx)        │
└───────────┬─────────────┬───────────────┘
            │             │
    ┌───────▼────┐ ┌──────▼────┐ ┌────────▼──────┐
    │ API Server │ │ API Server│ │ Export Worker │
    │  (REST)    │ │  (REST)   │ │  (Job Worker) │
    └────────────┘ └───────────┘ └───────────────┘
```

---

## 6. 监控与告警

### 6.1 关键指标
| 指标 | 告警阈值 |
|------|----------|
| 导出任务内存使用 | > 400MB |
| 导出任务耗时 | > 10分钟 |
| 数据库连接池使用率 | > 80% |
| 队列积压任务数 | > 100 |

### 6.2 OpenTelemetry 埋点
```csharp
// 导出性能追踪
using var activity = _activitySource.StartActivity("ExportReport");
activity?.SetTag("report.id", reportId);
activity?.SetTag("export.format", format);

// 记录导出指标
_exportCounter.Add(1, new KeyValuePair<string, object?>("format", format));
_exportDuration.Record(stopwatch.ElapsedMilliseconds);
```

---

## 7. 安全设计

### 7.1 数据脱敏
```csharp
public class DataMaskingService
{
    public object Mask(object value, MaskType type) => type switch
    {
        MaskType.Phone => MaskPhone(value?.ToString()),
        MaskType.IdCard => MaskIdCard(value?.ToString()),
        MaskType.BankCard => MaskBankCard(value?.ToString()),
        _ => value
    };
}
```

### 7.2 SQL 注入防护
- 强制使用参数化查询
- 禁止直接拼接 SQL
- 数据源 SQL 白名单审核

---

## 8. 总结

One-Report 通过以下设计实现高性能低资源占用：

1. **流式处理**：IAsyncEnumerable + 流式写入，内存与数据量无关
2. **连接池优化**：合理配置连接池，支持高并发查询
3. **分块处理**：大数据量分块读取/写入，控制内存峰值
4. **对象复用**：ArrayPool 复用缓冲区，减少 GC 压力
5. **按需加载**：报表元数据缓存，数据查询延迟加载
6. **资源限制**：容器化部署，严格限制 CPU/内存使用
