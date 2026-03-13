# one-blog 架构设计文档

## 1. 项目概述

**项目名称**: one-blog  
**项目类型**: 个人博客系统  
**目标用户**: 个人博主、技术写作者

### 核心功能
- 文章发布与管理（Markdown 支持）
- 分类与标签系统
- 评论功能
- 用户认证与授权
- 文章搜索
- 响应式前端界面

---

## 2. 技术栈

### 前端
| 技术 | 版本 | 用途 |
|------|------|------|
| React | 18.x | UI 框架 |
| TypeScript | 5.x | 类型安全 |
| Vite | 5.x | 构建工具 |
| TanStack Query | 5.x | 数据获取与缓存 |
| Zustand | 4.x | 状态管理 |
| React Router | 6.x | 路由管理 |
| Tailwind CSS | 3.x | 样式框架 |
| Axios | 1.x | HTTP 客户端 |

### 后端
| 技术 | 版本 | 用途 |
|------|------|------|
| .NET 8 | 8.x | 运行时 |
| Minimal API | - | 轻量级 API |
| EF Core 8 | 8.x | ORM |
| PostgreSQL | 16.x | 数据库 |
| JWT Bearer | - | 身份认证 |
| FluentValidation | 11.x | 输入验证 |
| AutoMapper | 12.x | 对象映射 |
| Serilog | 3.x | 日志记录 |

### 基础设施
| 技术 | 用途 |
|------|------|
| Docker | 容器化 |
| Docker Compose | 本地开发环境 |
| Nginx | 反向代理、静态文件服务 |
| GitHub Actions | CI/CD |

---

## 3. 系统架构

### 3.1 整体架构图

```
┌─────────────────────────────────────────────────────────────┐
│                         客户端层                             │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────────────┐ │
│  │  Web 浏览器  │  │  移动端浏览器 │  │  第三方 RSS/爬虫     │ │
│  └─────────────┘  └─────────────┘  └─────────────────────┘ │
└────────────────────────────┬────────────────────────────────┘
                             │
                             ▼
┌─────────────────────────────────────────────────────────────┐
│                         网关层                               │
│                     ┌─────────────┐                         │
│                     │    Nginx    │                         │
│                     │  (反向代理)  │                         │
│                     └─────────────┘                         │
└────────────────────────────┬────────────────────────────────┘
                             │
                             ▼
┌─────────────────────────────────────────────────────────────┐
│                         应用层                               │
│  ┌───────────────────────────────────────────────────────┐ │
│  │              .NET 8 Minimal API                        │ │
│  │  ┌───────────┐ ┌───────────┐ ┌───────────┐ ┌────────┐ │ │
│  │  │ 文章模块   │ │ 用户模块   │ │ 评论模块   │ │ 搜索模块│ │ │
│  │  └───────────┘ └───────────┘ └───────────┘ └────────┘ │ │
│  │  ┌───────────┐ ┌───────────┐ ┌───────────┐            │ │
│  │  │ 认证模块   │ │ 分类模块   │ │ 标签模块   │            │ │
│  │  └───────────┘ └───────────┘ └───────────┘            │ │
│  └───────────────────────────────────────────────────────┘ │
└────────────────────────────┬────────────────────────────────┘
                             │
                             ▼
┌─────────────────────────────────────────────────────────────┐
│                         数据层                               │
│  ┌─────────────┐  ┌─────────────────┐  ┌─────────────────┐ │
│  │  PostgreSQL │  │    Redis        │  │   文件存储       │ │
│  │  (主数据库)  │  │  (缓存/会话)     │  │  (图片/附件)     │ │
│  └─────────────┘  └─────────────────┘  └─────────────────┘ │
└─────────────────────────────────────────────────────────────┘
```

### 3.2 分层架构

```
┌─────────────────────────────────────────┐
│           Presentation Layer            │
│           (React SPA)                   │
├─────────────────────────────────────────┤
│           API Gateway Layer             │
│           (Nginx + Routing)             │
├─────────────────────────────────────────┤
│           Application Layer             │
│           (.NET 8 Minimal API)          │
│  ┌─────────┐ ┌─────────┐ ┌─────────┐   │
│  │  Controllers/APIs    │              │
│  │  (Minimal API Endpoints)            │
│  ├─────────┤ ├─────────┤ ├─────────┤   │
│  │  Application Services │             │
│  │  (业务逻辑编排)        │             │
│  ├─────────┤ ├─────────┤ ├─────────┤   │
│  │  Domain Services      │             │
│  │  (核心业务逻辑)        │             │
│  └─────────┘ └─────────┘ └─────────┘   │
├─────────────────────────────────────────┤
│           Data Access Layer             │
│           (EF Core + Repository)        │
├─────────────────────────────────────────┤
│           Infrastructure Layer          │
│           (日志、缓存、文件存储)          │
└─────────────────────────────────────────┘
```

---

## 4. 目录结构

```
one-blog/
├── 📁 docs/                          # 项目文档
│   ├── architecture.md               # 本文件
│   ├── api-contract.yaml             # OpenAPI 契约
│   └── database-schema.sql           # 数据库设计
│
├── 📁 src/                           # 源代码
│   ├── 📁 Blog.Api/                  # 后端 API 项目
│   │   ├── 📁 Controllers/           # API 控制器
│   │   ├── 📁 Models/                # DTO/ViewModel
│   │   ├── 📁 Services/              # 业务服务
│   │   ├── 📁 Data/                  # EF Core 上下文
│   │   ├── 📁 Entities/              # 领域实体
│   │   ├── 📁 Migrations/            # 数据库迁移
│   │   ├── 📁 Middleware/            # 中间件
│   │   ├── 📁 Configuration/         # 配置类
│   │   ├── Program.cs                # 入口程序
│   │   └── appsettings.json          # 配置文件
│   │
│   └── 📁 Blog.Web/                  # 前端项目
│       ├── 📁 src/
│       │   ├── 📁 components/        # React 组件
│       │   ├── 📁 pages/             # 页面组件
│       │   ├── 📁 hooks/             # 自定义 Hooks
│       │   ├── 📁 services/          # API 服务
│       │   ├── 📁 store/             # 状态管理
│       │   ├── 📁 types/             # TypeScript 类型
│       │   ├── 📁 utils/             # 工具函数
│       │   ├── 📁 styles/            # 样式文件
│       │   ├── App.tsx               # 应用入口
│       │   └── main.tsx              # 渲染入口
│       ├── index.html
│       ├── package.json
│       └── vite.config.ts
│
├── 📁 tests/                         # 测试项目
│   ├── 📁 Blog.Api.Tests/            # 后端单元测试
│   └── 📁 Blog.Web.Tests/            # 前端单元测试
│
├── 📁 docker/                        # Docker 配置
│   ├── Dockerfile.api                # API Dockerfile
│   ├── Dockerfile.web                # Web Dockerfile
│   └── nginx.conf                    # Nginx 配置
│
├── docker-compose.yml                # 本地开发编排
├── .gitignore
└── README.md
```

---

## 5. 模块设计

### 5.1 核心领域模型

```csharp
// 文章 (Post)
public class Post
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;  // URL 友好的标识
    public string Content { get; set; } = string.Empty;  // Markdown 内容
    public string? Summary { get; set; }  // 摘要
    public string? CoverImage { get; set; }  // 封面图
    public PostStatus Status { get; set; }  // 状态：草稿/已发布/归档
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? PublishedAt { get; set; }
    
    // 关联
    public Guid AuthorId { get; set; }
    public User Author { get; set; } = null!;
    public ICollection<Category> Categories { get; set; } = new List<Category>();
    public ICollection<Tag> Tags { get; set; } = new List<Tag>();
    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
}

// 用户 (User)
public class User
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? Avatar { get; set; }
    public string? Bio { get; set; }
    public UserRole Role { get; set; }  // Admin / Author / Reader
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; }
}

// 评论 (Comment)
public class Comment
{
    public Guid Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? AuthorName { get; set; }  // 匿名评论
    public string? AuthorEmail { get; set; }
    public DateTime CreatedAt { get; set; }
    public CommentStatus Status { get; set; }  // Pending / Approved / Rejected
    
    public Guid PostId { get; set; }
    public Post Post { get; set; } = null!;
    public Guid? UserId { get; set; }  // 注册用户
    public User? User { get; set; }
    public Guid? ParentId { get; set; }  // 回复评论
    public Comment? Parent { get; set; }
}

// 分类 (Category)
public class Category
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ICollection<Post> Posts { get; set; } = new List<Post>();
}

// 标签 (Tag)
public class Tag
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public ICollection<Post> Posts { get; set; } = new List<Post>();
}
```

### 5.2 模块职责

| 模块 | 职责 | 关键接口 |
|------|------|----------|
| **PostModule** | 文章 CRUD、Markdown 渲染、文章状态管理 | `IPostService` |
| **AuthModule** | JWT 认证、用户注册/登录、密码加密 | `IAuthService` |
| **CommentModule** | 评论管理、嵌套回复、评论审核 | `ICommentService` |
| **CategoryModule** | 分类管理、文章分类关联 | `ICategoryService` |
| **TagModule** | 标签管理、热门标签统计 | `ITagService` |
| **UserModule** | 用户资料管理、权限控制 | `IUserService` |

---

## 6. 非功能性需求

### 6.1 性能目标

| 指标 | 目标值 | 策略 |
|------|--------|------|
| API 响应时间 (P95) | < 200ms | 数据库索引、查询优化 |
| 首页加载时间 | < 1.5s | SSR/SSG、资源压缩、CDN |
| 并发用户支持 | 1000+ | 缓存、连接池、水平扩展 |
| 数据库查询 | < 50ms | 索引优化、分页加载 |

### 6.2 缓存策略

```
┌─────────────────────────────────────────────┐
│              多级缓存架构                     │
├─────────────────────────────────────────────┤
│  L1: In-Memory Cache (API 进程内)            │
│      - 热点数据 (配置、用户信息)              │
│      - TTL: 5 分钟                           │
├─────────────────────────────────────────────┤
│  L2: Redis (分布式缓存)                      │
│      - 文章列表、评论统计                     │
│      - TTL: 10 分钟                          │
├─────────────────────────────────────────────┤
│  L3: Response Caching (HTTP)                │
│      - 公开 API 响应                          │
│      - 文章详情页 CDN 缓存                    │
└─────────────────────────────────────────────┘
```

### 6.3 安全设计

| 层面 | 措施 |
|------|------|
| **认证** | JWT Bearer Token，Access Token (15min) + Refresh Token (7天) |
| **授权** | 基于角色的访问控制 (RBAC) |
| **输入** | FluentValidation 验证，XSS 过滤 |
| **输出** | 敏感数据脱敏，HTML 转义 |
| **传输** | HTTPS 强制，HSTS 头 |
| **存储** | 密码 bcrypt 加密，数据库连接字符串加密 |

### 6.4 扩展性设计

- **水平扩展**: 无状态 API 设计，支持多实例部署
- **数据库**: 读写分离准备，分表策略预留
- **文件存储**: 抽象存储接口，支持本地/OSS 切换

---

## 7. API 设计原则

### 7.1 RESTful 规范

- **资源命名**: 使用复数名词 `/api/posts`, `/api/users`
- **HTTP 方法**: GET(查询), POST(创建), PUT(全量更新), PATCH(部分更新), DELETE(删除)
- **状态码**: 200(成功), 201(创建), 400(参数错误), 401(未认证), 403(无权限), 404(不存在), 500(服务器错误)
- **分页**: `?page=1&pageSize=20`，返回 `{ data: [], total: 100, page: 1, pageSize: 20 }`

### 7.2 响应格式

```json
{
  "success": true,
  "data": { ... },
  "message": null,
  "errorCode": null
}
```

错误响应：

```json
{
  "success": false,
  "data": null,
  "message": "Validation failed",
  "errorCode": "VALIDATION_ERROR",
  "errors": {
    "title": ["Title is required"],
    "content": ["Content must be at least 10 characters"]
  }
}
```

---

## 8. 部署架构

### 8.1 容器化部署

```yaml
# docker-compose.yml 简化示意
version: '3.8'
services:
  postgres:
    image: postgres:16-alpine
    environment:
      POSTGRES_DB: blogdb
      POSTGRES_USER: bloguser
      POSTGRES_PASSWORD: ${DB_PASSWORD}
    volumes:
      - postgres_data:/var/lib/postgresql/data
      
  redis:
    image: redis:7-alpine
    
  api:
    build: ./src/Blog.Api
    environment:
      ConnectionStrings__Default: "Host=postgres;Database=blogdb;..."
      JWT__Secret: ${JWT_SECRET}
    depends_on:
      - postgres
      - redis
      
  web:
    build: ./src/Blog.Web
    
  nginx:
    image: nginx:alpine
    ports:
      - "80:80"
      - "443:443"
    volumes:
      - ./docker/nginx.conf:/etc/nginx/nginx.conf
    depends_on:
      - api
      - web
```

### 8.2 CI/CD 流程

```
┌──────────┐     ┌──────────┐     ┌──────────┐     ┌──────────┐
│  Push    │────▶│  Build   │────▶│   Test   │────▶│  Deploy  │
│  to main │     │  & Lint  │     │          │     │          │
└──────────┘     └──────────┘     └──────────┘     └──────────┘
                                                      │
                                 ┌────────────────────┴───────────┐
                                 ▼                                ▼
                          ┌────────────┐                  ┌────────────┐
                          │   Staging  │                  │ Production │
                          │  (auto)    │                  │  (manual)  │
                          └────────────┘                  └────────────┘
```

---

## 9. 监控与日志

### 9.1 日志策略

- **结构日志**: Serilog + JSON 格式
- **日志级别**: 
  - Development: Debug+
  - Production: Information+
- **关键日志**: API 请求/响应、异常、认证事件、数据变更

### 9.2 健康检查

```csharp
// 健康检查端点
GET /api/health
{
  "status": "healthy",
  "checks": {
    "database": "healthy",
    "redis": "healthy"
  },
  "timestamp": "2024-01-15T10:30:00Z"
}
```

---

## 10. 风险与缓解

| 风险 | 影响 | 缓解措施 |
|------|------|----------|
| 数据库性能瓶颈 | 高 | 索引优化、读写分离、缓存 |
| XSS/注入攻击 | 高 | 输入验证、输出转义、参数化查询 |
| 文件上传漏洞 | 中 | 文件类型白名单、大小限制、存储隔离 |
| 单点故障 | 中 | 多实例部署、数据库主从 |

---

## 11. 版本历史

| 版本 | 日期 | 变更 |
|------|------|------|
| v1.0 | 2024-01-15 | 初始架构设计 |
