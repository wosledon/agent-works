# One-Blog M3 API Documentation

## Backend M3 新功能

### 1. 评论系统 API (`/api/comments`)

#### 公共端点

| 方法 | 端点 | 描述 |
|------|------|------|
| GET | `/api/comments/post/{postId}` | 获取文章评论（分页） |
| GET | `/api/comments/post/{postId}/tree` | 获取嵌套评论树 |
| POST | `/api/comments/post/{postId}` | 发表评论（需审核） |

#### 管理端点（需要 Admin 角色）

| 方法 | 端点 | 描述 |
|------|------|------|
| GET | `/api/comments` | 获取所有评论（ moderation queue） |
| GET | `/api/comments/{id}` | 获取评论详情 |
| POST | `/api/comments/{id}/approve` | 审核通过评论 |
| POST | `/api/comments/{id}/reject` | 拒绝评论 |
| PUT | `/api/comments/{id}` | 更新评论 |
| DELETE | `/api/comments/{id}` | 删除评论 |
| POST | `/api/comments/bulk-approve` | 批量审核 |
| POST | `/api/comments/bulk-delete` | 批量删除 |
| GET | `/api/comments/statistics` | 获取评论统计 |

#### 评论请求示例
```json
POST /api/comments/post/1
{
  "content": "这是一篇很棒的文章！",
  "authorName": "访客用户",
  "authorEmail": "guest@example.com",
  "parentId": null
}
```

#### 回复嵌套限制
- 最多支持 2 层嵌套（评论 -> 回复）
- 回复不能继续被回复

### 2. 搜索功能 API (`/api/search`)

#### PostgreSQL 全文搜索

| 方法 | 端点 | 描述 |
|------|------|------|
| GET | `/api/search/articles?q={query}` | 文章全文搜索 |
| GET | `/api/search/advanced` | 高级搜索（多条件） |
| GET | `/api/search/suggestions?q={query}` | 搜索建议（自动完成） |
| GET | `/api/search/popular` | 热门搜索 |
| POST | `/api/search/clear-cache` | 清除搜索缓存 |

#### 全文搜索示例
```
GET /api/search/articles?q=dotnet tutorial&page=1&pageSize=10&sortBy=relevance
```

#### 高级搜索参数
```
GET /api/search/advanced?q=keyword&title=title&content=content&author=john&categoryId=1&tags=dotnet,csharp&fromDate=2024-01-01&toDate=2024-12-31
```

### 3. 性能优化

#### 缓存策略
- **文章缓存**: 10 分钟
- **文章列表**: 5 分钟
- **热门文章**: 15 分钟
- **最新文章**: 10 分钟
- **搜索建议**: 10 分钟
- **搜索结果**: 5 分钟

#### 数据库优化
- **GIN 索引**: PostgreSQL 全文搜索向量
- **复合索引**: 
  - `Posts(Status, PublishedAt)`
  - `Comments(Status, PostId)`
  - `Comments(PostId, ParentId)`

#### 响应优化
- **Response Compression**: 启用 Gzip/Brotli
- **Response Caching**: 静态内容缓存
- **Query Splitting**: EF Core SplitQuery 模式
- **AsNoTracking**: 只读查询优化

#### CORS 配置
```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});
```

### 4. 缓存服务

#### ICacheService 接口
```csharp
public interface ICacheService
{
    Task<T?> GetAsync<T>(string key);
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null);
    Task RemoveAsync(string key);
    Task<bool> ExistsAsync(string key);
}
```

#### 实现
- **RedisCacheService**: 生产环境，分布式缓存
- **MemoryCacheService**: 开发环境，内存缓存

### 5. 缓存文章服务

#### IArticleService 接口
```csharp
public interface IArticleService
{
    Task<ArticleDto?> GetArticleBySlugAsync(string slug);
    Task<PagedArticlesResult> GetPublishedArticlesAsync(int page, int pageSize);
    Task<List<ArticleSummaryDto>> GetPopularArticlesAsync(int limit);
    Task<List<ArticleSummaryDto>> GetRecentArticlesAsync(int limit);
    Task InvalidateArticleCacheAsync(string slug);
    Task InvalidateArticleListCacheAsync();
}
```

### 6. PostgreSQL 全文搜索设置

#### 迁移创建的内容
```sql
-- 添加搜索向量列
ALTER TABLE "Posts" ADD COLUMN "SearchVector" tsvector;

-- 创建 GIN 索引
CREATE INDEX "IX_Posts_SearchVector" ON "Posts" USING GIN ("SearchVector");

-- 创建触发器函数
CREATE OR REPLACE FUNCTION update_posts_search_vector()
RETURNS TRIGGER AS $$
BEGIN
    NEW."SearchVector" := 
        setweight(to_tsvector('simple', COALESCE(NEW."Title", '')), 'A') ||
        setweight(to_tsvector('simple', COALESCE(NEW."Content", '')), 'B') ||
        setweight(to_tsvector('simple', COALESCE(NEW."Excerpt", '')), 'C');
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- 创建自动更新触发器
CREATE TRIGGER update_posts_search_vector_trigger
    BEFORE INSERT OR UPDATE ON "Posts"
    FOR EACH ROW
    EXECUTE FUNCTION update_posts_search_vector();
```

### 7. API 端点总览

```
/api/articles       - 文章管理（已存在）
/api/comments       - 评论系统（新增）
/api/search         - 搜索功能（新增）
/api/auth           - 认证（已存在）
/api/categories     - 分类（已存在）
```

### 8. 测试命令

```bash
# 运行 API
cd backend/backend
dotnet run

# 访问 Swagger UI
open http://localhost:5000/swagger

# 健康检查
GET /health
```

## 完成状态

✅ **评论系统 API** - 发表评论、嵌套回复、评论列表、审核管理
✅ **搜索功能 API** - PostgreSQL 全文搜索、高级搜索、搜索建议
✅ **性能优化** - 缓存、查询优化、响应压缩、CORS

---

**Backend M3 完成**
