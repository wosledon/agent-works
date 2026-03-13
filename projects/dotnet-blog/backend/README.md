# DotnetBlog - 后端 API

基于 .NET 8 + EF Core + PostgreSQL 的博客系统后端。

## 技术栈

- **框架**: .NET 8
- **API**: Minimal API
- **ORM**: Entity Framework Core 8
- **数据库**: PostgreSQL
- **文档**: Swagger / OpenAPI

## 项目结构

```
backend/
├── Data/
│   └── BlogDbContext.cs          # EF Core DbContext
├── Endpoints/
│   ├── PostEndpoints.cs          # 文章 API 端点
│   └── CategoryEndpoints.cs      # 分类 API 端点
├── Models/
│   ├── Post.cs                   # 文章实体
│   ├── Category.cs               # 分类实体
│   ├── Tag.cs                    # 标签实体
│   ├── Comment.cs                # 评论实体
│   └── User.cs                   # 用户实体
├── Program.cs                    # 应用入口
├── appsettings.json              # 配置文件
├── appsettings.Development.json  # 开发配置
├── DotnetBlog.csproj             # 项目文件
└── README.md                     # 说明文档
```

## 快速开始

### 1. 安装依赖

```bash
dotnet restore
```

### 2. 配置数据库

确保 PostgreSQL 正在运行，并创建数据库：

```sql
CREATE DATABASE dotnetblog;
```

或开发环境使用 docker：

```bash
docker run -d \
  --name postgres-blog \
  -e POSTGRES_USER=postgres \
  -e POSTGRES_PASSWORD=postgres \
  -e POSTGRES_DB=dotnetblog \
  -p 5432:5432 \
  postgres:15
```

### 3. 运行数据库迁移

```bash
dotnet ef migrations add InitialCreate
dotnet ef database update
```

### 4. 运行应用

```bash
dotnet run
```

应用将在 `http://localhost:5000` 启动。

Swagger 文档: `http://localhost:5000/swagger`

## API 端点

| 方法 | 路径 | 描述 |
|------|------|------|
| GET | `/api/posts` | 获取所有已发布文章 |
| GET | `/api/posts/{slug}` | 根据 slug 获取文章 |
| POST | `/api/posts` | 创建新文章 |
| GET | `/api/categories` | 获取所有分类 |
| GET | `/api/categories/{slug}/posts` | 获取分类下的文章 |

## 实体关系

```
Post (文章)
├── belongs to: Category (分类)
├── belongs to: User (作者)
├── has many: Tag (标签) [多对多]
└── has many: Comment (评论)

Comment (评论)
├── belongs to: Post
└── has many: Comment (回复) [自引用]
```

## 待办事项

- [ ] JWT 认证
- [ ] 文章搜索功能
- [ ] 分页支持
- [ ] 缓存层 (Redis)
- [ ] FluentValidation
- [ ] 单元测试
