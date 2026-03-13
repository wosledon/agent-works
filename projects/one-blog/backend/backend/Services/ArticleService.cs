using DotnetBlog.Data;
using DotnetBlog.Models;
using Microsoft.EntityFrameworkCore;

namespace DotnetBlog.Services;

/// <summary>
/// Cached article service for performance optimization
/// </summary>
public interface IArticleService
{
    Task<ArticleDto?> GetArticleBySlugAsync(string slug, CancellationToken ct = default);
    Task<PagedArticlesResult> GetPublishedArticlesAsync(int page, int pageSize, CancellationToken ct = default);
    Task<List<ArticleSummaryDto>> GetPopularArticlesAsync(int limit, CancellationToken ct = default);
    Task<List<ArticleSummaryDto>> GetRecentArticlesAsync(int limit, CancellationToken ct = default);
    Task InvalidateArticleCacheAsync(string slug);
    Task InvalidateArticleListCacheAsync();
}

public class CachedArticleService : IArticleService
{
    private readonly BlogDbContext _dbContext;
    private readonly ICacheService _cache;
    private readonly ILogger<CachedArticleService> _logger;

    // Cache keys
    private const string ArticleKeyPrefix = "article:";
    private const string ArticleListKey = "articles:list";
    private const string PopularArticlesKey = "articles:popular";
    private const string RecentArticlesKey = "articles:recent";

    public CachedArticleService(
        BlogDbContext dbContext,
        ICacheService cache,
        ILogger<CachedArticleService> logger)
    {
        _dbContext = dbContext;
        _cache = cache;
        _logger = logger;
    }

    public async Task<ArticleDto?> GetArticleBySlugAsync(string slug, CancellationToken ct = default)
    {
        var cacheKey = $"{ArticleKeyPrefix}{slug}";
        
        // Try cache first
        var cached = await _cache.GetAsync<ArticleDto>(cacheKey);
        if (cached != null)
        {
            _logger.LogDebug("Cache hit for article: {Slug}", slug);
            return cached;
        }

        // Load from database
        var article = await _dbContext.Posts
            .AsNoTracking()
            .Where(p => p.Status == PostStatus.Published && p.Slug == slug)
            .Include(p => p.Author)
            .Include(p => p.Category)
            .Include(p => p.Tags)
            .FirstOrDefaultAsync(ct);

        if (article == null)
            return null;

        // Increment view count (fire and forget)
        _ = Task.Run(async () =>
        {
            await _dbContext.Posts
                .Where(p => p.Id == article.Id)
                .ExecuteUpdateAsync(setters =>
                    setters.SetProperty(p => p.ViewCount, p => p.ViewCount + 1));
        });

        var dto = MapToDto(article);

        // Cache for 10 minutes
        await _cache.SetAsync(cacheKey, dto, TimeSpan.FromMinutes(10));
        _logger.LogDebug("Cached article: {Slug}", slug);

        return dto;
    }

    public async Task<PagedArticlesResult> GetPublishedArticlesAsync(int page, int pageSize, CancellationToken ct = default)
    {
        var cacheKey = $"{ArticleListKey}:{page}:{pageSize}";
        
        var cached = await _cache.GetAsync<PagedArticlesResult>(cacheKey);
        if (cached != null)
        {
            return cached;
        }

        var query = _dbContext.Posts
            .AsNoTracking()
            .Where(p => p.Status == PostStatus.Published)
            .Include(p => p.Author)
            .Include(p => p.Category)
            .Include(p => p.Tags)
            .OrderByDescending(p => p.PublishedAt);

        var totalCount = await query.CountAsync(ct);

        var articles = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new ArticleSummaryDto
            {
                Id = p.Id,
                Title = p.Title,
                Slug = p.Slug,
                Excerpt = p.Excerpt,
                CoverImage = p.CoverImage,
                PublishedAt = p.PublishedAt ?? p.CreatedAt,
                ViewCount = p.ViewCount,
                Author = new AuthorSummaryDto
                {
                    Id = p.Author.Id,
                    Username = p.Author.Username,
                    DisplayName = p.Author.DisplayName ?? p.Author.Username,
                    Avatar = p.Author.Avatar
                },
                Category = new CategorySummaryDto
                {
                    Id = p.Category.Id,
                    Name = p.Category.Name,
                    Slug = p.Category.Slug
                },
                Tags = p.Tags.Select(t => new TagSummaryDto
                {
                    Id = t.Id,
                    Name = t.Name,
                    Slug = t.Slug
                }).ToList()
            })
            .ToListAsync(ct);

        var result = new PagedArticlesResult
        {
            Items = articles,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };

        // Cache for 5 minutes
        await _cache.SetAsync(cacheKey, result, TimeSpan.FromMinutes(5));

        return result;
    }

    public async Task<List<ArticleSummaryDto>> GetPopularArticlesAsync(int limit, CancellationToken ct = default)
    {
        var cached = await _cache.GetAsync<List<ArticleSummaryDto>>(PopularArticlesKey);
        if (cached != null)
        {
            return cached;
        }

        var articles = await _dbContext.Posts
            .AsNoTracking()
            .Where(p => p.Status == PostStatus.Published)
            .Include(p => p.Author)
            .Include(p => p.Category)
            .OrderByDescending(p => p.ViewCount)
            .Take(limit)
            .Select(p => new ArticleSummaryDto
            {
                Id = p.Id,
                Title = p.Title,
                Slug = p.Slug,
                Excerpt = p.Excerpt,
                CoverImage = p.CoverImage,
                PublishedAt = p.PublishedAt ?? p.CreatedAt,
                ViewCount = p.ViewCount,
                Author = new AuthorSummaryDto
                {
                    Id = p.Author.Id,
                    Username = p.Author.Username,
                    DisplayName = p.Author.DisplayName ?? p.Author.Username,
                    Avatar = p.Author.Avatar
                },
                Category = new CategorySummaryDto
                {
                    Id = p.Category.Id,
                    Name = p.Category.Name,
                    Slug = p.Category.Slug
                },
                Tags = p.Tags.Select(t => new TagSummaryDto
                {
                    Id = t.Id,
                    Name = t.Name,
                    Slug = t.Slug
                }).ToList()
            })
            .ToListAsync(ct);

        // Cache for 15 minutes
        await _cache.SetAsync(PopularArticlesKey, articles, TimeSpan.FromMinutes(15));

        return articles;
    }

    public async Task<List<ArticleSummaryDto>> GetRecentArticlesAsync(int limit, CancellationToken ct = default)
    {
        var cached = await _cache.GetAsync<List<ArticleSummaryDto>>(RecentArticlesKey);
        if (cached != null)
        {
            return cached;
        }

        var articles = await _dbContext.Posts
            .AsNoTracking()
            .Where(p => p.Status == PostStatus.Published)
            .Include(p => p.Author)
            .Include(p => p.Category)
            .Include(p => p.Tags)
            .OrderByDescending(p => p.PublishedAt)
            .Take(limit)
            .Select(p => new ArticleSummaryDto
            {
                Id = p.Id,
                Title = p.Title,
                Slug = p.Slug,
                Excerpt = p.Excerpt,
                CoverImage = p.CoverImage,
                PublishedAt = p.PublishedAt ?? p.CreatedAt,
                ViewCount = p.ViewCount,
                Author = new AuthorSummaryDto
                {
                    Id = p.Author.Id,
                    Username = p.Author.Username,
                    DisplayName = p.Author.DisplayName ?? p.Author.Username,
                    Avatar = p.Author.Avatar
                },
                Category = new CategorySummaryDto
                {
                    Id = p.Category.Id,
                    Name = p.Category.Name,
                    Slug = p.Category.Slug
                },
                Tags = p.Tags.Select(t => new TagSummaryDto
                {
                    Id = t.Id,
                    Name = t.Name,
                    Slug = t.Slug
                }).ToList()
            })
            .ToListAsync(ct);

        // Cache for 10 minutes
        await _cache.SetAsync(RecentArticlesKey, articles, TimeSpan.FromMinutes(10));

        return articles;
    }

    public async Task InvalidateArticleCacheAsync(string slug)
    {
        await _cache.RemoveAsync($"{ArticleKeyPrefix}{slug}");
        _logger.LogDebug("Invalidated cache for article: {Slug}", slug);
    }

    public async Task InvalidateArticleListCacheAsync()
    {
        // Note: In a real implementation with Redis, you'd use patterns to clear
        // For now, we'll just remove the known keys
        await _cache.RemoveAsync(ArticleListKey);
        await _cache.RemoveAsync(PopularArticlesKey);
        await _cache.RemoveAsync(RecentArticlesKey);
        _logger.LogDebug("Invalidated article list caches");
    }

    private static ArticleDto MapToDto(Post article)
    {
        return new ArticleDto
        {
            Id = article.Id,
            Title = article.Title,
            Slug = article.Slug,
            Content = article.Content,
            Excerpt = article.Excerpt,
            CoverImage = article.CoverImage,
            Status = article.Status.ToString(),
            PublishedAt = article.PublishedAt,
            UpdatedAt = article.UpdatedAt,
            ViewCount = article.ViewCount,
            Author = new AuthorSummaryDto
            {
                Id = article.Author.Id,
                Username = article.Author.Username,
                DisplayName = article.Author.DisplayName ?? article.Author.Username,
                Avatar = article.Author.Avatar
            },
            Category = new CategorySummaryDto
            {
                Id = article.Category.Id,
                Name = article.Category.Name,
                Slug = article.Category.Slug
            },
            Tags = article.Tags.Select(t => new TagSummaryDto
            {
                Id = t.Id,
                Name = t.Name,
                Slug = t.Slug
            }).ToList()
        };
    }
}

// DTOs
public class ArticleDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? Excerpt { get; set; }
    public string? CoverImage { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? PublishedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int ViewCount { get; set; }
    public AuthorSummaryDto Author { get; set; } = null!;
    public CategorySummaryDto Category { get; set; } = null!;
    public List<TagSummaryDto> Tags { get; set; } = new();
}

public class ArticleSummaryDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Excerpt { get; set; }
    public string? CoverImage { get; set; }
    public DateTime PublishedAt { get; set; }
    public int ViewCount { get; set; }
    public AuthorSummaryDto Author { get; set; } = null!;
    public CategorySummaryDto Category { get; set; } = null!;
    public List<TagSummaryDto> Tags { get; set; } = new();
}

public class AuthorSummaryDto
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Avatar { get; set; }
}

public class CategorySummaryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
}

public class TagSummaryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
}

public class PagedArticlesResult
{
    public List<ArticleSummaryDto> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasNextPage => Page < TotalPages;
    public bool HasPreviousPage => Page > 1;
}
