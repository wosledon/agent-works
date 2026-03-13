using DotnetBlog.Data;
using DotnetBlog.Models;
using Microsoft.EntityFrameworkCore;

namespace DotnetBlog.Endpoints;

public static class PostEndpoints
{
    public static IEndpointRouteBuilder MapPostEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/posts")
            .WithTags("Posts");

        group.MapGet("/", async (BlogDbContext db, CancellationToken ct) =>
        {
            var posts = await db.Posts
                .AsNoTracking()
                .Where(p => p.Status == PostStatus.Published)
                .Include(p => p.Author)
                .Include(p => p.Category)
                .Include(p => p.Tags)
                .OrderByDescending(p => p.PublishedAt)
                .Select(p => new
                {
                    p.Id,
                    p.Title,
                    p.Slug,
                    p.Excerpt,
                    p.CoverImage,
                    p.PublishedAt,
                    Author = p.Author.DisplayName ?? p.Author.Username,
                    Category = p.Category.Name,
                    Tags = p.Tags.Select(t => t.Name).ToList()
                })
                .ToListAsync(ct);

            return Results.Ok(posts);
        })
        .WithName("GetPosts")
        .WithSummary("Get all published posts");

        group.MapGet("/{slug}", async (string slug, BlogDbContext db, CancellationToken ct) =>
        {
            var post = await db.Posts
                .AsNoTracking()
                .Include(p => p.Author)
                .Include(p => p.Category)
                .Include(p => p.Tags)
                .FirstOrDefaultAsync(p => p.Slug == slug, ct);

            if (post is null)
                return Results.NotFound();

            return Results.Ok(post);
        })
        .WithName("GetPostBySlug")
        .WithSummary("Get post by slug");

        group.MapPost("/", async (CreatePostRequest request, BlogDbContext db, CancellationToken ct) =>
        {
            var post = new Post
            {
                Title = request.Title,
                Slug = request.Slug,
                Content = request.Content,
                Excerpt = request.Excerpt,
                CategoryId = request.CategoryId,
                AuthorId = request.AuthorId,
                Status = request.Publish ? PostStatus.Published : PostStatus.Draft,
                PublishedAt = request.Publish ? DateTime.UtcNow : null
            };

            db.Posts.Add(post);
            await db.SaveChangesAsync(ct);

            return Results.Created($"/api/posts/{post.Slug}", post);
        })
        .WithName("CreatePost")
        .WithSummary("Create a new post");

        return app;
    }
}

public record CreatePostRequest(
    string Title,
    string Slug,
    string Content,
    string? Excerpt,
    int CategoryId,
    int AuthorId,
    bool Publish = false
);
