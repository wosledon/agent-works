using DotnetBlog.Data;
using DotnetBlog.Models;
using DotnetBlog.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NpgsqlTypes;

namespace DotnetBlog.Endpoints;

public static class SearchEndpoints
{
    public static IEndpointRouteBuilder MapSearchEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/search")
            .WithTags("Search");

        // ========== FULL-TEXT SEARCH ==========

        // Search articles with PostgreSQL full-text search
        group.MapGet("/articles", async (
            [FromQuery] string q,
            BlogDbContext db,
            ICacheService cache,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? sortBy = "relevance", // relevance, date, views
            CancellationToken ct = default) =>
        {
            if (string.IsNullOrWhiteSpace(q))
                return Results.BadRequest(new { error = "Search query is required" });

            var searchTerm = q.Trim();
            if (searchTerm.Length < 2)
                return Results.BadRequest(new { error = "Search query must be at least 2 characters" });

            // Check cache first
            var cacheKey = $"search:{searchTerm}:{page}:{pageSize}:{sortBy}";
            var cached = await cache.GetAsync<SearchResult<ArticleSummaryResponse>>(cacheKey);
            if (cached != null)
            {
                return Results.Ok(cached);
            }

            // Build PostgreSQL full-text search query
            var normalizedQuery = NormalizeSearchQuery(searchTerm);
            
            var query = db.Posts
                .AsNoTracking()
                .Where(p => p.Status == PostStatus.Published)
                .Include(p => p.Author)
                .Include(p => p.Category)
                .Include(p => p.Tags)
                .AsQueryable();

            // Use PostgreSQL full-text search if available
            if (IsPostgreSQL(db))
            {
                var searchResults = await db.Posts
                    .FromSqlInterpolated($"SELECT p.*, ts_rank(to_tsvector('simple', COALESCE(p.title, '') || ' ' || COALESCE(p.content, '')), plainto_tsquery('simple', {normalizedQuery})) as rank FROM \"Posts\" p WHERE p.\"Status\" = 1 AND to_tsvector('simple', COALESCE(p.title, '') || ' ' || COALESCE(p.content, '')) @@ plainto_tsquery('simple', {normalizedQuery})")
                    .Include(p => p.Author)
                    .Include(p => p.Category)
                    .Include(p => p.Tags)
                    .ToListAsync(ct);

                var totalCount = searchResults.Count;
                
                var paginatedResults = searchResults
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(p => new ArticleSummaryResponse(
                        p.Id,
                        p.Title,
                        p.Slug,
                        HighlightSearchTerms(p.Excerpt, searchTerm),
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
                    .ToList();

                var result = new SearchResult<ArticleSummaryResponse>
                {
                    Items = paginatedResults,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize,
                    Query = searchTerm,
                    SearchTimeMs = 0 // Would be measured in real implementation
                };

                // Cache for 5 minutes
                await cache.SetAsync(cacheKey, result, TimeSpan.FromMinutes(5));
                
                return Results.Ok(result);
            }
            else
            {
                // Fallback to LIKE search for SQLite
                var searchPattern = $"%{searchTerm}%";
                
                var fallbackQuery = query.Where(p =>
                    EF.Functions.Like(p.Title, searchPattern) ||
                    EF.Functions.Like(p.Content, searchPattern) ||
                    (p.Excerpt != null && EF.Functions.Like(p.Excerpt, searchPattern)));

                var totalCount = await fallbackQuery.CountAsync(ct);

                // Apply sorting
                fallbackQuery = sortBy?.ToLower() switch
                {
                    "date" => fallbackQuery.OrderByDescending(p => p.PublishedAt ?? p.CreatedAt),
                    "views" => fallbackQuery.OrderByDescending(p => p.ViewCount),
                    _ => fallbackQuery.OrderByDescending(p => p.PublishedAt ?? p.CreatedAt) // Default to date for SQLite
                };

                var articles = await fallbackQuery
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(p => new ArticleSummaryResponse(
                        p.Id,
                        p.Title,
                        p.Slug,
                        HighlightSearchTerms(p.Excerpt, searchTerm),
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

                var result = new SearchResult<ArticleSummaryResponse>
                {
                    Items = articles,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize,
                    Query = searchTerm,
                    SearchTimeMs = 0
                };

                // Cache for 5 minutes
                await cache.SetAsync(cacheKey, result, TimeSpan.FromMinutes(5));

                return Results.Ok(result);
            }
        })
        .WithName("SearchArticles")
        .WithSummary("Full-text search articles")
        .Produces<SearchResult<ArticleSummaryResponse>>(200)
        .Produces(400);

        // Advanced search with filters
        group.MapGet("/advanced", async (
            [FromQuery] string? q,
            [FromQuery] string? title,
            [FromQuery] string? content,
            [FromQuery] string? author,
            [FromQuery] int? categoryId,
            [FromQuery] string? tags,
            [FromQuery] DateTime? fromDate,
            [FromQuery] DateTime? toDate,
            BlogDbContext db,
            ICacheService cache,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            CancellationToken ct = default) =>
        {
            // Check cache
            var cacheKey = $"search:adv:{q}:{title}:{content}:{author}:{categoryId}:{tags}:{fromDate}:{toDate}:{page}:{pageSize}";
            var cached = await cache.GetAsync<SearchResult<ArticleSummaryResponse>>(cacheKey);
            if (cached != null)
            {
                return Results.Ok(cached);
            }

            var query = db.Posts
                .AsNoTracking()
                .Where(p => p.Status == PostStatus.Published)
                .Include(p => p.Author)
                .Include(p => p.Category)
                .Include(p => p.Tags)
                .AsQueryable();

            // Apply filters
            if (!string.IsNullOrWhiteSpace(q))
            {
                var pattern = $"%{q.Trim()}%";
                query = query.Where(p =>
                    EF.Functions.Like(p.Title, pattern) ||
                    EF.Functions.Like(p.Content, pattern) ||
                    (p.Excerpt != null && EF.Functions.Like(p.Excerpt, pattern)));
            }

            if (!string.IsNullOrWhiteSpace(title))
            {
                var pattern = $"%{title.Trim()}%";
                query = query.Where(p => EF.Functions.Like(p.Title, pattern));
            }

            if (!string.IsNullOrWhiteSpace(content))
            {
                var pattern = $"%{content.Trim()}%";
                query = query.Where(p => EF.Functions.Like(p.Content, pattern));
            }

            if (!string.IsNullOrWhiteSpace(author))
            {
                var authorPattern = $"%{author.Trim()}%";
                query = query.Where(p =>
                    EF.Functions.Like(p.Author.Username, authorPattern) ||
                    EF.Functions.Like(p.Author.DisplayName ?? "", authorPattern));
            }

            if (categoryId.HasValue)
            {
                query = query.Where(p => p.CategoryId == categoryId.Value);
            }

            if (!string.IsNullOrWhiteSpace(tags))
            {
                var tagSlugs = tags.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(t => t.Trim().ToLower())
                    .ToList();
                
                query = query.Where(p => p.Tags.Any(t => tagSlugs.Contains(t.Slug.ToLower())));
            }

            if (fromDate.HasValue)
            {
                query = query.Where(p => p.PublishedAt >= fromDate.Value || p.CreatedAt >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                query = query.Where(p => p.PublishedAt <= toDate.Value || p.CreatedAt <= toDate.Value);
            }

            var totalCount = await query.CountAsync(ct);

            var articles = await query
                .OrderByDescending(p => p.PublishedAt ?? p.CreatedAt)
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

            var result = new SearchResult<ArticleSummaryResponse>
            {
                Items = articles,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                Query = q ?? "advanced search",
                SearchTimeMs = 0
            };

            // Cache for 5 minutes
            await cache.SetAsync(cacheKey, result, TimeSpan.FromMinutes(5));

            return Results.Ok(result);
        })
        .WithName("AdvancedSearch")
        .WithSummary("Advanced search with multiple filters")
        .Produces<SearchResult<ArticleSummaryResponse>>(200);

        // Quick search suggestions (autocomplete)
        group.MapGet("/suggestions", async (
            [FromQuery] string q,
            BlogDbContext db,
            ICacheService cache,
            [FromQuery] int limit = 5,
            CancellationToken ct = default) =>
        {
            if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
                return Results.Ok(new List<SearchSuggestion>());

            var searchTerm = q.Trim();
            var cacheKey = $"search:suggestions:{searchTerm}:{limit}";
            
            var cached = await cache.GetAsync<List<SearchSuggestion>>(cacheKey);
            if (cached != null)
            {
                return Results.Ok(cached);
            }

            var pattern = $"%{searchTerm}%";

            // Search in titles
            var titleSuggestions = await db.Posts
                .AsNoTracking()
                .Where(p => p.Status == PostStatus.Published && EF.Functions.Like(p.Title, pattern))
                .OrderByDescending(p => p.ViewCount)
                .Take(limit)
                .Select(p => new SearchSuggestion
                {
                    Type = "article",
                    Text = p.Title,
                    Value = p.Slug,
                    Url = $"/articles/{p.Slug}"
                })
                .ToListAsync(ct);

            // Search in tags
            var tagSuggestions = await db.Tags
                .AsNoTracking()
                .Where(t => EF.Functions.Like(t.Name, pattern))
                .Take(limit)
                .Select(t => new SearchSuggestion
                {
                    Type = "tag",
                    Text = $"#{t.Name}",
                    Value = t.Slug,
                    Url = $"/tags/{t.Slug}"
                })
                .ToListAsync(ct);

            // Search in categories
            var categorySuggestions = await db.Categories
                .AsNoTracking()
                .Where(c => EF.Functions.Like(c.Name, pattern))
                .Take(limit)
                .Select(c => new SearchSuggestion
                {
                    Type = "category",
                    Text = c.Name,
                    Value = c.Slug,
                    Url = $"/categories/{c.Slug}"
                })
                .ToListAsync(ct);

            var suggestions = titleSuggestions
                .Concat(tagSuggestions)
                .Concat(categorySuggestions)
                .Take(limit)
                .ToList();

            // Cache for 10 minutes (suggestions don't change often)
            await cache.SetAsync(cacheKey, suggestions, TimeSpan.FromMinutes(10));

            return Results.Ok(suggestions);
        })
        .WithName("SearchSuggestions")
        .WithSummary("Get search suggestions (autocomplete)")
        .Produces<List<SearchSuggestion>>(200);

        // Popular searches (stats)
        group.MapGet("/popular", async (
            BlogDbContext db,
            [FromQuery] int limit = 10,
            CancellationToken ct = default) =>
        {
            // Return popular tags and categories as "popular searches"
            var popularTags = await db.Tags
                .AsNoTracking()
                .Where(t => t.Posts.Any())
                .OrderByDescending(t => t.Posts.Count)
                .Take(limit / 2)
                .Select(t => new PopularSearch
                {
                    Term = t.Name,
                    Type = "tag",
                    Count = t.Posts.Count
                })
                .ToListAsync(ct);

            var popularCategories = await db.Categories
                .AsNoTracking()
                .Where(c => c.Posts.Any(p => p.Status == PostStatus.Published))
                .OrderByDescending(c => c.Posts.Count(p => p.Status == PostStatus.Published))
                .Take(limit / 2)
                .Select(c => new PopularSearch
                {
                    Term = c.Name,
                    Type = "category",
                    Count = c.Posts.Count(p => p.Status == PostStatus.Published)
                })
                .ToListAsync(ct);

            var result = popularTags.Concat(popularCategories)
                .OrderByDescending(p => p.Count)
                .Take(limit)
                .ToList();

            return Results.Ok(result);
        })
        .WithName("PopularSearches")
        .WithSummary("Get popular search terms")
        .Produces<List<PopularSearch>>(200);

        // Clear search cache (admin only)
        group.MapPost("/clear-cache", [Authorize(Roles = "Admin")] async (
            ICacheService cache,
            CancellationToken ct) =>
        {
            // Note: This is a simplified implementation
            // In production, you'd want to clear only search-related cache keys
            await cache.RemoveAsync("search:*");
            return Results.Ok(new { message = "Search cache cleared" });
        })
        .WithName("ClearSearchCache")
        .WithSummary("Clear search cache (Admin only)")
        .RequireAuthorization();

        return app;
    }

    private static bool IsPostgreSQL(BlogDbContext db)
    {
        return db.Database.ProviderName?.Contains("PostgreSQL") ?? false;
    }

    private static string NormalizeSearchQuery(string query)
    {
        // Remove special characters and normalize for PostgreSQL full-text search
        var normalized = query
            .Replace("\\", " ")
            .Replace("&", " ")
            .Replace("|", " ")
            .Replace("!", " ")
            .Replace("(", " ")
            .Replace(")", " ")
            .Replace(":", " ")
            .Replace("*", " ")
            .Replace("'", " '")
            .Trim();
        
        // Split into words and join with & for AND search
        var words = normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return string.Join(" & ", words);
    }

    private static string? HighlightSearchTerms(string? text, string searchTerm)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        // Simple highlighting - in production you'd want more sophisticated logic
        var terms = searchTerm.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var result = text;
        
        foreach (var term in terms)
        {
            if (term.Length < 2) continue;
            
            // Case-insensitive replace
            var index = result.IndexOf(term, StringComparison.OrdinalIgnoreCase);
            if (index >= 0)
            {
                var original = result.Substring(index, term.Length);
                result = result.Substring(0, index) + $"**{original}**" + result.Substring(index + term.Length);
            }
        }

        return result;
    }
}

// Response models
public class SearchResult<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public string Query { get; set; } = string.Empty;
    public int SearchTimeMs { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasNextPage => Page < TotalPages;
    public bool HasPreviousPage => Page > 1;
}

public class SearchSuggestion
{
    public string Type { get; set; } = string.Empty; // article, tag, category
    public string Text { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
}

public class PopularSearch
{
    public string Term { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public int Count { get; set; }
}

// Note: Reusing ArticleSummaryResponse, AuthorResponse, CategoryResponse, TagResponse 
// from ArticleEndpoints namespace (DotnetBlog.Endpoints)
