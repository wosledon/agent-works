using DotnetBlog.Data;
using DotnetBlog.Models;
using DotnetBlog.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace DotnetBlog.Endpoints;

public static class ArticleEndpoints
{
    public static IEndpointRouteBuilder MapArticleEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/articles")
            .WithTags("Articles");

        // ========== PUBLIC ENDPOINTS ==========

        // Get all published articles (public)
        group.MapGet("/", async (
            BlogDbContext db,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? search = null,
            [FromQuery] int? categoryId = null,
            [FromQuery] string? tag = null,
            CancellationToken ct = default) =>
        {
            var query = db.Posts
                .AsNoTracking()
                .Where(p => p.Status == PostStatus.Published)
                .Include(p => p.Author)
                .Include(p => p.Category)
                .Include(p => p.Tags)
                .AsQueryable();

            // Apply filters
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(p =>
                    p.Title.Contains(search) ||
                    (p.Excerpt != null && p.Excerpt.Contains(search)));
            }

            if (categoryId.HasValue)
            {
                query = query.Where(p => p.CategoryId == categoryId.Value);
            }

            if (!string.IsNullOrWhiteSpace(tag))
            {
                query = query.Where(p => p.Tags.Any(t => t.Slug == tag));
            }

            var totalCount = await query.CountAsync(ct);

            var articles = await query
                .OrderByDescending(p => p.PublishedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new ArticleSummaryResponse(
                    p.Id,
                    p.Title,
                    p.Slug,
                    p.Excerpt,
                    p.CoverImage,
                    p.PublishedAt ?? p.CreatedAt,
                    p.ViewCount,
                    new AuthorResponse(
                        p.Author.Id,
                        p.Author.Username,
                        p.Author.DisplayName ?? p.Author.Username,
                        p.Author.Avatar
                    ),
                    new CategoryResponse(
                        p.Category.Id,
                        p.Category.Name,
                        p.Category.Slug
                    ),
                    p.Tags.Select(t => new TagResponse(t.Id, t.Name, t.Slug)).ToList()
                ))
                .ToListAsync(ct);

            return Results.Ok(new PagedResponse<ArticleSummaryResponse>(
                articles,
                totalCount,
                page,
                pageSize
            ));
        })
        .WithName("GetArticles")
        .WithSummary("Get published articles with filtering and pagination")
        .Produces<PagedResponse<ArticleSummaryResponse>>(200);

        // Get article by slug (public)
        group.MapGet("/{slug}", async (string slug, BlogDbContext db, CancellationToken ct) =>
        {
            var article = await db.Posts
                .AsNoTracking()
                .Where(p => p.Status == PostStatus.Published && p.Slug == slug)
                .Include(p => p.Author)
                .Include(p => p.Category)
                .Include(p => p.Tags)
                .Include(p => p.Comments.Where(c => c.ParentId == null))
                    .ThenInclude(c => c.Replies)
                .FirstOrDefaultAsync(ct);

            if (article is null)
                return Results.NotFound(new { error = "Article not found" });

            // Increment view count
            await db.Posts
                .Where(p => p.Id == article.Id)
                .ExecuteUpdateAsync(setters =>
                    setters.SetProperty(p => p.ViewCount, p => p.ViewCount + 1), ct);

            var response = new ArticleDetailResponse(
                article.Id,
                article.Title,
                article.Slug,
                article.Content,
                article.Excerpt,
                article.CoverImage,
                article.Status.ToString(),
                article.PublishedAt,
                article.UpdatedAt,
                article.ViewCount,
                new AuthorResponse(
                    article.Author.Id,
                    article.Author.Username,
                    article.Author.DisplayName ?? article.Author.Username,
                    article.Author.Avatar
                ),
                new CategoryResponse(
                    article.Category.Id,
                    article.Category.Name,
                    article.Category.Slug
                ),
                article.Tags.Select(t => new TagResponse(t.Id, t.Name, t.Slug)).ToList(),
                article.Comments.Select(c => MapCommentToResponse(c)).ToList()
            );

            return Results.Ok(response);
        })
        .WithName("GetArticleBySlug")
        .WithSummary("Get published article by slug")
        .Produces<ArticleDetailResponse>(200)
        .Produces(404);

        // ========== AUTHENTICATED ENDPOINTS ==========

        // Get my articles (authenticated)
        group.MapGet("/my/articles", [Authorize] async (
            BlogDbContext db,
            ClaimsPrincipal user,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? status = null,
            CancellationToken ct = default) =>
        {
            var userId = GetUserId(user);
            if (!userId.HasValue)
                return Results.Unauthorized();

            var query = db.Posts
                .AsNoTracking()
                .Where(p => p.AuthorId == userId.Value)
                .Include(p => p.Category)
                .Include(p => p.Tags)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<PostStatus>(status, true, out var postStatus))
            {
                query = query.Where(p => p.Status == postStatus);
            }

            var totalCount = await query.CountAsync(ct);

            var articles = await query
                .OrderByDescending(p => p.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new ArticleSummaryResponse(
                    p.Id,
                    p.Title,
                    p.Slug,
                    p.Excerpt,
                    p.CoverImage,
                    p.PublishedAt ?? p.CreatedAt,
                    p.ViewCount,
                    null!, // Skip author for my articles
                    new CategoryResponse(p.Category.Id, p.Category.Name, p.Category.Slug),
                    p.Tags.Select(t => new TagResponse(t.Id, t.Name, t.Slug)).ToList()
                ))
                .ToListAsync(ct);

            return Results.Ok(new PagedResponse<ArticleSummaryResponse>(
                articles, totalCount, page, pageSize));
        })
        .WithName("GetMyArticles")
        .WithSummary("Get current user's articles")
        .RequireAuthorization()
        .Produces<PagedResponse<ArticleSummaryResponse>>(200)
        .Produces(401);

        // Get article by ID for editing (authenticated - must be author or admin)
        group.MapGet("/{id:int}", [Authorize] async (int id, BlogDbContext db, ClaimsPrincipal user, CancellationToken ct) =>
        {
            var userId = GetUserId(user);
            var userRole = GetUserRole(user);
            if (!userId.HasValue)
                return Results.Unauthorized();

            var article = await db.Posts
                .AsNoTracking()
                .Where(p => p.Id == id)
                .Include(p => p.Author)
                .Include(p => p.Category)
                .Include(p => p.Tags)
                .FirstOrDefaultAsync(ct);

            if (article is null)
                return Results.NotFound(new { error = "Article not found" });

            // Check permission
            if (article.AuthorId != userId.Value && userRole != UserRole.Admin)
                return Results.Forbid();

            return Results.Ok(new ArticleEditResponse(
                article.Id,
                article.Title,
                article.Slug,
                article.Content,
                article.Excerpt,
                article.CoverImage,
                article.Status.ToString(),
                article.PublishedAt,
                article.UpdatedAt,
                article.CategoryId,
                article.Tags.Select(t => t.Id).ToList()
            ));
        })
        .WithName("GetArticleById")
        .WithSummary("Get article by ID for editing")
        .RequireAuthorization()
        .Produces<ArticleEditResponse>(200)
        .Produces(401)
        .Produces(403)
        .Produces(404);

        // Create article (authenticated)
        group.MapPost("/", [Authorize] async (CreateArticleRequest request, BlogDbContext db, ClaimsPrincipal user, CancellationToken ct) =>
        {
            var userId = GetUserId(user);
            if (!userId.HasValue)
                return Results.Unauthorized();

            // Validate slug uniqueness
            if (await db.Posts.AnyAsync(p => p.Slug == request.Slug, ct))
            {
                return Results.BadRequest(new { error = "Slug already exists" });
            }

            // Validate category exists
            if (!await db.Categories.AnyAsync(c => c.Id == request.CategoryId, ct))
            {
                return Results.BadRequest(new { error = "Category not found" });
            }

            var article = new Post
            {
                Title = request.Title,
                Slug = request.Slug,
                Content = request.Content,
                Excerpt = request.Excerpt,
                CoverImage = request.CoverImage,
                CategoryId = request.CategoryId,
                AuthorId = userId.Value,
                Status = request.Publish ? PostStatus.Published : PostStatus.Draft,
                PublishedAt = request.Publish ? DateTime.UtcNow : null,
                CreatedAt = DateTime.UtcNow
            };

            // Add tags if provided
            if (request.TagIds?.Any() == true)
            {
                var tags = await db.Tags
                    .Where(t => request.TagIds.Contains(t.Id))
                    .ToListAsync(ct);
                article.Tags = tags;
            }

            db.Posts.Add(article);
            await db.SaveChangesAsync(ct);

            return Results.Created($"/api/articles/{article.Slug}", new { id = article.Id, slug = article.Slug });
        })
        .WithName("CreateArticle")
        .WithSummary("Create a new article")
        .RequireAuthorization()
        .Produces<object>(201)
        .Produces(400)
        .Produces(401);

        // Update article (authenticated - must be author or admin)
        group.MapPut("/{id:int}", [Authorize] async (int id, UpdateArticleRequest request, BlogDbContext db, ClaimsPrincipal user, CancellationToken ct) =>
        {
            var userId = GetUserId(user);
            var userRole = GetUserRole(user);
            if (!userId.HasValue)
                return Results.Unauthorized();

            var article = await db.Posts
                .Include(p => p.Tags)
                .FirstOrDefaultAsync(p => p.Id == id, ct);

            if (article is null)
                return Results.NotFound(new { error = "Article not found" });

            // Check permission
            if (article.AuthorId != userId.Value && userRole != UserRole.Admin)
                return Results.Forbid();

            // Validate slug uniqueness (if changed)
            if (article.Slug != request.Slug && await db.Posts.AnyAsync(p => p.Slug == request.Slug && p.Id != id, ct))
            {
                return Results.BadRequest(new { error = "Slug already exists" });
            }

            // Validate category exists
            if (!await db.Categories.AnyAsync(c => c.Id == request.CategoryId, ct))
            {
                return Results.BadRequest(new { error = "Category not found" });
            }

            // Update fields
            article.Title = request.Title;
            article.Slug = request.Slug;
            article.Content = request.Content;
            article.Excerpt = request.Excerpt;
            article.CoverImage = request.CoverImage;
            article.CategoryId = request.CategoryId;
            article.UpdatedAt = DateTime.UtcNow;

            // Update publish status
            if (request.Publish && article.Status != PostStatus.Published)
            {
                article.Status = PostStatus.Published;
                article.PublishedAt ??= DateTime.UtcNow;
            }
            else if (!request.Publish)
            {
                article.Status = PostStatus.Draft;
            }

            // Update tags
            if (request.TagIds is not null)
            {
                article.Tags.Clear();
                if (request.TagIds.Any())
                {
                    var tags = await db.Tags
                        .Where(t => request.TagIds.Contains(t.Id))
                        .ToListAsync(ct);
                    foreach (var tag in tags)
                    {
                        article.Tags.Add(tag);
                    }
                }
            }

            await db.SaveChangesAsync(ct);

            return Results.Ok(new { id = article.Id, slug = article.Slug });
        })
        .WithName("UpdateArticle")
        .WithSummary("Update an article")
        .RequireAuthorization()
        .Produces<object>(200)
        .Produces(400)
        .Produces(401)
        .Produces(403)
        .Produces(404);

        // Delete article (authenticated - must be author or admin)
        group.MapDelete("/{id:int}", [Authorize] async (int id, BlogDbContext db, ClaimsPrincipal user, CancellationToken ct) =>
        {
            var userId = GetUserId(user);
            var userRole = GetUserRole(user);
            if (!userId.HasValue)
                return Results.Unauthorized();

            var article = await db.Posts.FindAsync(new object[] { id }, ct);

            if (article is null)
                return Results.NotFound(new { error = "Article not found" });

            // Check permission
            if (article.AuthorId != userId.Value && userRole != UserRole.Admin)
                return Results.Forbid();

            db.Posts.Remove(article);
            await db.SaveChangesAsync(ct);

            return Results.NoContent();
        })
        .WithName("DeleteArticle")
        .WithSummary("Delete an article")
        .RequireAuthorization()
        .Produces(204)
        .Produces(401)
        .Produces(403)
        .Produces(404);

        // Publish article (authenticated - must be author or admin)
        group.MapPost("/{id:int}/publish", [Authorize] async (int id, BlogDbContext db, ClaimsPrincipal user, CancellationToken ct) =>
        {
            var userId = GetUserId(user);
            var userRole = GetUserRole(user);
            if (!userId.HasValue)
                return Results.Unauthorized();

            var article = await db.Posts.FindAsync(new object[] { id }, ct);

            if (article is null)
                return Results.NotFound(new { error = "Article not found" });

            // Check permission
            if (article.AuthorId != userId.Value && userRole != UserRole.Admin)
                return Results.Forbid();

            article.Status = PostStatus.Published;
            article.PublishedAt ??= DateTime.UtcNow;
            article.UpdatedAt = DateTime.UtcNow;

            await db.SaveChangesAsync(ct);

            return Results.Ok(new { id = article.Id, status = article.Status.ToString() });
        })
        .WithName("PublishArticle")
        .WithSummary("Publish an article")
        .RequireAuthorization()
        .Produces<object>(200)
        .Produces(401)
        .Produces(403)
        .Produces(404);

        // Unpublish article (authenticated - must be author or admin)
        group.MapPost("/{id:int}/unpublish", [Authorize] async (int id, BlogDbContext db, ClaimsPrincipal user, CancellationToken ct) =>
        {
            var userId = GetUserId(user);
            var userRole = GetUserRole(user);
            if (!userId.HasValue)
                return Results.Unauthorized();

            var article = await db.Posts.FindAsync(new object[] { id }, ct);

            if (article is null)
                return Results.NotFound(new { error = "Article not found" });

            // Check permission
            if (article.AuthorId != userId.Value && userRole != UserRole.Admin)
                return Results.Forbid();

            article.Status = PostStatus.Draft;
            article.UpdatedAt = DateTime.UtcNow;

            await db.SaveChangesAsync(ct);

            return Results.Ok(new { id = article.Id, status = article.Status.ToString() });
        })
        .WithName("UnpublishArticle")
        .WithSummary("Unpublish an article")
        .RequireAuthorization()
        .Produces<object>(200)
        .Produces(401)
        .Produces(403)
        .Produces(404);

        return app;
    }

    private static int? GetUserId(ClaimsPrincipal user)
    {
        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (int.TryParse(userIdClaim, out var userId))
            return userId;
        return null;
    }

    private static UserRole GetUserRole(ClaimsPrincipal user)
    {
        var roleClaim = user.FindFirst(ClaimTypes.Role)?.Value;
        if (Enum.TryParse<UserRole>(roleClaim, out var role))
            return role;
        return UserRole.Reader;
    }

    private static CommentResponse MapCommentToResponse(Comment comment)
    {
        return new CommentResponse(
            comment.Id,
            comment.Content,
            comment.AuthorName,
            comment.CreatedAt,
            comment.Replies.Select(r => MapCommentToResponse(r)).ToList()
        );
    }
}

// Request/Response Records
public record CreateArticleRequest(
    string Title,
    string Slug,
    string Content,
    string? Excerpt,
    string? CoverImage,
    int CategoryId,
    List<int>? TagIds,
    bool Publish = false
);

public record UpdateArticleRequest(
    string Title,
    string Slug,
    string Content,
    string? Excerpt,
    string? CoverImage,
    int CategoryId,
    List<int>? TagIds,
    bool Publish = false
);

public record ArticleSummaryResponse(
    int Id,
    string Title,
    string Slug,
    string? Excerpt,
    string? CoverImage,
    DateTime PublishedAt,
    int ViewCount,
    AuthorResponse? Author,
    CategoryResponse Category,
    List<TagResponse> Tags
);

public record ArticleDetailResponse(
    int Id,
    string Title,
    string Slug,
    string Content,
    string? Excerpt,
    string? CoverImage,
    string Status,
    DateTime? PublishedAt,
    DateTime? UpdatedAt,
    int ViewCount,
    AuthorResponse Author,
    CategoryResponse Category,
    List<TagResponse> Tags,
    List<CommentResponse> Comments
);

public record ArticleEditResponse(
    int Id,
    string Title,
    string Slug,
    string Content,
    string? Excerpt,
    string? CoverImage,
    string Status,
    DateTime? PublishedAt,
    DateTime? UpdatedAt,
    int CategoryId,
    List<int> TagIds
);

public record AuthorResponse(
    int Id,
    string Username,
    string DisplayName,
    string? Avatar
);

public record CategoryResponse(
    int Id,
    string Name,
    string Slug
);

public record TagResponse(
    int Id,
    string Name,
    string Slug
);

public record CommentResponse(
    int Id,
    string Content,
    string AuthorName,
    DateTime CreatedAt,
    List<CommentResponse> Replies
);

public record PagedResponse<T>(
    List<T> Items,
    int TotalCount,
    int Page,
    int PageSize
)
{
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasNextPage => Page < TotalPages;
    public bool HasPreviousPage => Page > 1;
}
