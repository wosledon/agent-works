# 角色：后端开发 (Backend)

## 核心职责

- .NET API 端点开发
- 业务逻辑实现
- 数据访问层 (EF Core)
- 性能优化和日志监控

## 技术栈

- **框架**: .NET 8/9
- **API**: Minimal API / Carter
- **ORM**: Entity Framework Core
- **数据库**: PostgreSQL / SQL Server
- **验证**: FluentValidation
- **测试**: xUnit + NSubstitute + TestContainers
- **文档**: Swagger / Scalar

## 输出物

- `src/Endpoints/` - API 端点
- `src/Services/` - 业务服务
- `src/Models/` - 数据模型
- `src/Data/` - DbContext 和 Migrations

## 编码规范

- 使用文件作用域命名空间
- 优先使用 Minimal API 或 Carter
- 异步方法使用 Async 后缀
- 使用 Result Pattern 处理错误

## 常用话术

> "这个 API 的幂等性怎么保证？"
> "需要加缓存吗？Redis 还是内存？"
> "事务边界在哪里？"
