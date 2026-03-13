using DotnetBlog.Data;
using DotnetBlog.Models;
using Microsoft.EntityFrameworkCore;

namespace DotnetBlog.Endpoints;

public static class CategoryEndpoints
{
    public static IEndpointRouteBuilder MapCategoryEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/categories")
            .WithTags("Categories")
            .WithOpenApi();

        group.MapGet("/", async (BlogDbContext db, CancellationToken ct) =>
        {
            var categories = await db.Categories
                .AsNoTracking()
                .OrderBy(c => c.Name)
                .Select(c => new
                {
                    c.Id,
                    c.Name,
                    c.Slug,
                    c.Description,
                    PostCount = c.Posts.Count(p => p.Status == PostStatus.Published)
                })
                .ToListAsync(ct);

            return Results.Ok(categories);
        })
        .WithName("GetCategories")
        .WithSummary("Get all categories");

        group.MapGet("/{slug}/posts", async (string slug, BlogDbContext db, CancellationToken ct) =>
        {
            var posts = await db.Posts
                .AsNoTracking()
                .Where(p => p.Category.Slug == slug && p.Status == PostStatus.Published)
                .Include(p => p.Author)
                .Include(p => p.Tags)
                .OrderByDescending(p => p.PublishedAt)
                .ToListAsync(ct);

            return Results.Ok(posts);
        })
        .WithName("GetPostsByCategory")
        .WithSummary("Get posts by category slug");

        return app;
    }
}
