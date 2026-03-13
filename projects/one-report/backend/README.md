# OneReport - 低代码报表工具后端

基于 .NET 8 + EF Core + PostgreSQL 的低代码报表平台后端，支持流式导出和大批量数据处理。

## 技术栈

- **.NET 8** - Web API 框架
- **EF Core 8** - ORM
- **PostgreSQL** - 数据库
- **Npgsql** - PostgreSQL .NET 驱动
- **ClosedXML** - Excel 导出
- **CsvHelper** - CSV 导出
- **Swashbuckle** - OpenAPI/Swagger 文档

## 项目结构

```
src/OneReport/
├── Controllers/          # API 控制器
│   ├── ReportDefinitionsController.cs  # 报表定义管理
│   ├── ReportDataController.cs         # 报表数据查询
│   ├── ReportExportsController.cs      # 报表导出
│   └── DataSourcesController.cs        # 数据源管理
├── Data/
│   ├── Entities/         # 实体类
│   ├── Configurations/   # EF Core 配置
│   ├── Migrations/       # 数据库迁移
│   └── AppDbContext.cs   # 数据库上下文
├── Models/
│   ├── DTOs/            # 数据传输对象
│   ├── Requests/        # 请求模型
│   └── Responses/       # 响应模型
├── Services/
│   ├── Interfaces/      # 服务接口
│   └── Implementations/ # 服务实现
├── Common/              # 通用工具
├── Program.cs           # 应用入口
└── appsettings.json     # 配置文件
```

## 核心特性

### 1. 流式导出 (低内存)

- **CsvExport**: 使用 IAsyncEnumerable 流式处理百万级数据
- **ExcelExport**: 使用 ClosedXML 分批写入
- **JsonExport**: 使用 Utf8JsonWriter 流式序列化

```csharp
// 流式导出示例
await foreach (var row in _dataService.StreamDataAsync(reportId, parameters))
{
    // 逐行处理，内存占用低
    csvWriter.WriteField(row["field"]);
}
```

### 2. 数据库优化

- **无跟踪查询**: `AsNoTracking()` 提升只读性能
- **连接池**: 最小5，最大100连接
- **批量操作**: MaxBatchSize=100
- **SnakeCase 命名**: PostgreSQL 标准命名

### 3. 报表功能

- 动态 SQL 查询
- 列定义与格式化
- 分页预览
- 异步导出任务
- 多格式导出 (CSV, Excel, JSON)

## 快速开始

### 环境要求

- .NET 8 SDK
- PostgreSQL 14+

### 1. 克隆项目

```bash
cd /root/.openclaw/workspace/agent-works/projects/one-report/backend
```

### 2. 配置数据库

编辑 `src/OneReport/appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=one_report_dev;Username=postgres;Password=your_password"
  }
}
```

### 3. 运行迁移

```bash
cd src/OneReport
dotnet ef database update
```

### 4. 运行项目

```bash
dotnet run
```

访问 Swagger 文档: `http://localhost:5000/swagger`

## Docker 部署

```bash
# 使用 Docker Compose
cd backend
docker-compose up -d

# 服务地址
# API: http://localhost:5000
# PostgreSQL: localhost:5432
```

## API 端点

### 报表定义

| 方法 | 端点 | 说明 |
|------|------|------|
| GET | `/api/reportdefinitions` | 获取报表列表 |
| GET | `/api/reportdefinitions/{id}` | 获取报表详情 |
| POST | `/api/reportdefinitions` | 创建报表 |
| PUT | `/api/reportdefinitions/{id}` | 更新报表 |
| DELETE | `/api/reportdefinitions/{id}` | 删除报表 |

### 报表数据

| 方法 | 端点 | 说明 |
|------|------|------|
| POST | `/api/reportdata/preview` | 预览数据（分页） |
| POST | `/api/reportdata/count` | 获取记录数 |
| POST | `/api/reportdata/query` | 执行原始查询 |

### 报表导出

| 方法 | 端点 | 说明 |
|------|------|------|
| POST | `/api/reportexports/download` | 同步导出下载 |
| POST | `/api/reportexports/jobs` | 创建异步导出任务 |
| GET | `/api/reportexports/jobs/{exportId}` | 查询导出进度 |
| GET | `/api/reportexports/jobs/{exportId}/download` | 下载导出文件 |

### 数据源

| 方法 | 端点 | 说明 |
|------|------|------|
| GET | `/api/datasources` | 获取数据源列表 |
| POST | `/api/datasources` | 创建数据源 |
| POST | `/api/datasources/test-connection` | 测试连接 |

## 导出请求示例

### 同步导出 CSV

```bash
curl -X POST http://localhost:5000/api/reportexports/download \
  -H "Content-Type: application/json" \
  -d '{
    "reportDefinitionId": "uuid-here",
    "format": "csv",
    "parameters": { "startDate": "2024-01-01" }
  }' \
  --output report.csv
```

### 创建异步导出任务

```bash
curl -X POST http://localhost:5000/api/reportexports/jobs \
  -H "Content-Type: application/json" \
  -d '{
    "reportDefinitionId": "uuid-here",
    "format": "excel",
    "fileName": "sales_report"
  }'
```

## 内存优化配置

```xml
<!-- OneReport.csproj -->
<PropertyGroup>
  <!-- 禁用服务器 GC，使用工作站 GC 降低内存占用 -->
  <ServerGarbageCollection>false</ServerGarbageCollection>
  <!-- 启用并发 GC -->
  <ConcurrentGarbageCollection>true</ConcurrentGarbageCollection>
</PropertyGroup>
```

## 许可证

MIT
