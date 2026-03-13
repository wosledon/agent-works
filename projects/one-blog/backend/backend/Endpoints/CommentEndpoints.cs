using DotnetBlog.Data;
using DotnetBlog.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace DotnetBlog.Endpoints;

public static class CommentEndpoints
{
    public static IEndpointRouteBuilder MapCommentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/comments")
            .WithTags("Comments");

        // ========== PUBLIC ENDPOINTS ==========

        // Get comments for a post (public)
        group.MapGet("/post/{postId:int}", async (
            int postId,
            BlogDbContext db,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken ct = default) =>
        {
            // Check if post exists and is published
            var postExists = await db.Posts
                .AsNoTracking()
                .AnyAsync(p => p.Id == postId && p.Status == PostStatus.Published, ct);

            if (!postExists)
                return Results.NotFound(new { error = "Post not found" });

            // Get only approved top-level comments with their replies
            var query = db.Comments
                .AsNoTracking()
                .Where(c => c.PostId == postId && c.ParentId == null && c.Status == CommentStatus.Approved)
                .Include(c => c.Replies.Where(r => r.Status == CommentStatus.Approved))
                .OrderByDescending(c => c.CreatedAt);

            var totalCount = await query.CountAsync(ct);

            var comments = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(c => new CommentDto
                {
                    Id = c.Id,
                    Content = c.Content,
                    AuthorName = c.AuthorName ?? "Anonymous",
                    CreatedAt = c.CreatedAt,
                    Replies = c.Replies.Select(r => new CommentDto
                    {
                        Id = r.Id,
                        Content = r.Content,
                        AuthorName = r.AuthorName ?? "Anonymous",
                        CreatedAt = r.CreatedAt,
                        Replies = new List<CommentDto>() // Replies are limited to 2 levels
                    }).ToList()
                })
                .ToListAsync(ct);

            return Results.Ok(new PagedResponse<CommentDto>(
                comments,
                totalCount,
                page,
                pageSize
            ));
        })
        .WithName("GetPostComments")
        .WithSummary("Get approved comments for a post")
        .Produces<PagedResponse<CommentDto>>(200)
        .Produces(404);

        // Get comment tree for a post (nested structure)
        group.MapGet("/post/{postId:int}/tree", async (
            int postId,
            BlogDbContext db,
            CancellationToken ct = default) =>
        {
            // Check if post exists and is published
            var postExists = await db.Posts
                .AsNoTracking()
                .AnyAsync(p => p.Id == postId && p.Status == PostStatus.Published, ct);

            if (!postExists)
                return Results.NotFound(new { error = "Post not found" });

            // Get all approved comments for this post
            var allComments = await db.Comments
                .AsNoTracking()
                .Where(c => c.PostId == postId && c.Status == CommentStatus.Approved)
                .OrderBy(c => c.CreatedAt)
                .Select(c => new CommentFlatDto
                {
                    Id = c.Id,
                    Content = c.Content,
                    AuthorName = c.AuthorName ?? "Anonymous",
                    ParentId = c.ParentId,
                    CreatedAt = c.CreatedAt
                })
                .ToListAsync(ct);

            // Build tree structure
            var commentTree = BuildCommentTree(allComments);

            return Results.Ok(new { comments = commentTree, totalCount = allComments.Count });
        })
        .WithName("GetPostCommentTree")
        .WithSummary("Get nested comment tree for a post")
        .Produces<object>(200)
        .Produces(404);

        // ========== COMMENT SUBMISSION (PUBLIC) ==========

        // Create a new comment (public - requires moderation)
        group.MapPost("/post/{postId:int}", async (
            int postId,
            CreateCommentRequest request,
            BlogDbContext db,
            CancellationToken ct) =>
        {
            // Check if post exists and is published
            var post = await db.Posts
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == postId && p.Status == PostStatus.Published, ct);

            if (post is null)
                return Results.NotFound(new { error = "Post not found" });

            // Validate request
            if (string.IsNullOrWhiteSpace(request.Content))
                return Results.BadRequest(new { error = "Content is required" });

            if (request.Content.Length > 5000)
                return Results.BadRequest(new { error = "Content exceeds maximum length of 5000 characters" });

            // Validate parent comment if provided
            if (request.ParentId.HasValue)
            {
                var parentExists = await db.Comments
                    .AnyAsync(c => c.Id == request.ParentId.Value && c.PostId == postId, ct);

                if (!parentExists)
                    return Results.BadRequest(new { error = "Parent comment not found" });

                // Prevent deeply nested replies (max 2 levels)
                var parentComment = await db.Comments
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.Id == request.ParentId.Value, ct);

                if (parentComment?.ParentId != null)
                    return Results.BadRequest(new { error = "Cannot reply to a reply. Maximum nesting depth is 2 levels." });
            }

            // Validate author info for guest comments
            if (string.IsNullOrWhiteSpace(request.AuthorName))
                return Results.BadRequest(new { error = "Author name is required" });

            if (!string.IsNullOrWhiteSpace(request.AuthorEmail) && !IsValidEmail(request.AuthorEmail))
                return Results.BadRequest(new { error = "Invalid email format" });

            var comment = new Comment
            {
                Content = request.Content.Trim(),
                AuthorName = request.AuthorName.Trim(),
                AuthorEmail = request.AuthorEmail?.Trim(),
                PostId = postId,
                ParentId = request.ParentId,
                Status = CommentStatus.Pending, // Require moderation
                CreatedAt = DateTime.UtcNow
            };

            db.Comments.Add(comment);
            await db.SaveChangesAsync(ct);

            return Results.Created($"/api/comments/{comment.Id}", new
            {
                message = "Comment submitted successfully and is pending moderation",
                comment = new CommentDto
                {
                    Id = comment.Id,
                    Content = comment.Content,
                    AuthorName = comment.AuthorName ?? "Anonymous",
                    CreatedAt = comment.CreatedAt,
                    Replies = new List<CommentDto>()
                }
            });
        })
        .WithName("CreateComment")
        .WithSummary("Submit a new comment (requires moderation)")
        .Produces<object>(201)
        .Produces(400)
        .Produces(404);

        // ========== AUTHENTICATED ENDPOINTS ==========

        // Get comment by ID (authenticated - admin only)
        group.MapGet("/{id:int}", [Authorize(Roles = "Admin")] async (
            int id,
            BlogDbContext db,
            CancellationToken ct) =>
        {
            var comment = await db.Comments
                .AsNoTracking()
                .Include(c => c.Post)
                .FirstOrDefaultAsync(c => c.Id == id, ct);

            if (comment is null)
                return Results.NotFound(new { error = "Comment not found" });

            return Results.Ok(new CommentDetailDto
            {
                Id = comment.Id,
                Content = comment.Content,
                AuthorName = comment.AuthorName,
                AuthorEmail = comment.AuthorEmail,
                Status = comment.Status.ToString(),
                CreatedAt = comment.CreatedAt,
                PostId = comment.PostId,
                PostTitle = comment.Post.Title,
                ParentId = comment.ParentId
            });
        })
        .WithName("GetCommentById")
        .WithSummary("Get comment by ID (Admin only)")
        .RequireAuthorization()
        .Produces<CommentDetailDto>(200)
        .Produces(401)
        .Produces(403)
        .Produces(404);

        // Get all comments (authenticated - admin only, with moderation queue)
        group.MapGet("/", [Authorize(Roles = "Admin")] async (
            BlogDbContext db,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? status = null,
            [FromQuery] int? postId = null,
            CancellationToken ct = default) =>
        {
            var query = db.Comments
                .AsNoTracking()
                .Include(c => c.Post)
                .AsQueryable();

            // Filter by status
            if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<CommentStatus>(status, true, out var commentStatus))
            {
                query = query.Where(c => c.Status == commentStatus);
            }

            // Filter by post
            if (postId.HasValue)
            {
                query = query.Where(c => c.PostId == postId.Value);
            }

            var totalCount = await query.CountAsync(ct);

            var comments = await query
                .OrderByDescending(c => c.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(c => new CommentDetailDto
                {
                    Id = c.Id,
                    Content = c.Content.Length > 100 ? c.Content.Substring(0, 100) + "..." : c.Content,
                    AuthorName = c.AuthorName,
                    AuthorEmail = c.AuthorEmail,
                    Status = c.Status.ToString(),
                    CreatedAt = c.CreatedAt,
                    PostId = c.PostId,
                    PostTitle = c.Post.Title,
                    ParentId = c.ParentId
                })
                .ToListAsync(ct);

            // Get pending count for moderation queue indicator
            var pendingCount = await db.Comments
                .CountAsync(c => c.Status == CommentStatus.Pending, ct);

            return Results.Ok(new
            {
                comments = new PagedResponse<CommentDetailDto>(comments, totalCount, page, pageSize),
                moderationQueue = new { pendingCount }
            });
        })
        .WithName("GetAllComments")
        .WithSummary("Get all comments with filtering (Admin only)")
        .RequireAuthorization()
        .Produces<object>(200)
        .Produces(401)
        .Produces(403);

        // Approve comment (authenticated - admin only)
        group.MapPost("/{id:int}/approve", [Authorize(Roles = "Admin")] async (
            int id,
            BlogDbContext db,
            CancellationToken ct) =>
        {
            var comment = await db.Comments.FindAsync(new object[] { id }, ct);

            if (comment is null)
                return Results.NotFound(new { error = "Comment not found" });

            comment.Status = CommentStatus.Approved;
            await db.SaveChangesAsync(ct);

            return Results.Ok(new { id = comment.Id, status = comment.Status.ToString() });
        })
        .WithName("ApproveComment")
        .WithSummary("Approve a pending comment (Admin only)")
        .RequireAuthorization()
        .Produces<object>(200)
        .Produces(401)
        .Produces(403)
        .Produces(404);

        // Reject comment (authenticated - admin only)
        group.MapPost("/{id:int}/reject", [Authorize(Roles = "Admin")] async (
            int id,
            BlogDbContext db,
            CancellationToken ct) =>
        {
            var comment = await db.Comments.FindAsync(new object[] { id }, ct);

            if (comment is null)
                return Results.NotFound(new { error = "Comment not found" });

            comment.Status = CommentStatus.Rejected;
            await db.SaveChangesAsync(ct);

            return Results.Ok(new { id = comment.Id, status = comment.Status.ToString() });
        })
        .WithName("RejectComment")
        .WithSummary("Reject a comment (Admin only)")
        .RequireAuthorization()
        .Produces<object>(200)
        .Produces(401)
        .Produces(403)
        .Produces(404);

        // Delete comment (authenticated - admin only)
        group.MapDelete("/{id:int}", [Authorize(Roles = "Admin")] async (
            int id,
            BlogDbContext db,
            CancellationToken ct) =>
        {
            var comment = await db.Comments.FindAsync(new object[] { id }, ct);

            if (comment is null)
                return Results.NotFound(new { error = "Comment not found" });

            // Delete will cascade to replies due to FK constraint
            db.Comments.Remove(comment);
            await db.SaveChangesAsync(ct);

            return Results.NoContent();
        })
        .WithName("DeleteComment")
        .WithSummary("Delete a comment and its replies (Admin only)")
        .RequireAuthorization()
        .Produces(204)
        .Produces(401)
        .Produces(403)
        .Produces(404);

        // Update comment (authenticated - admin only)
        group.MapPut("/{id:int}", [Authorize(Roles = "Admin")] async (
            int id,
            UpdateCommentRequest request,
            BlogDbContext db,
            CancellationToken ct) =>
        {
            var comment = await db.Comments.FindAsync(new object[] { id }, ct);

            if (comment is null)
                return Results.NotFound(new { error = "Comment not found" });

            if (string.IsNullOrWhiteSpace(request.Content))
                return Results.BadRequest(new { error = "Content is required" });

            comment.Content = request.Content.Trim();
            await db.SaveChangesAsync(ct);

            return Results.Ok(new { id = comment.Id, content = comment.Content });
        })
        .WithName("UpdateComment")
        .WithSummary("Update a comment (Admin only)")
        .RequireAuthorization()
        .Produces<object>(200)
        .Produces(400)
        .Produces(401)
        .Produces(403)
        .Produces(404);

        // Bulk approve comments (authenticated - admin only)
        group.MapPost("/bulk-approve", [Authorize(Roles = "Admin")] async (
            BulkActionRequest request,
            BlogDbContext db,
            CancellationToken ct) =>
        {
            if (request.Ids == null || request.Ids.Count == 0)
                return Results.BadRequest(new { error = "No comment IDs provided" });

            var comments = await db.Comments
                .Where(c => request.Ids.Contains(c.Id))
                .ToListAsync(ct);

            foreach (var comment in comments)
            {
                comment.Status = CommentStatus.Approved;
            }

            await db.SaveChangesAsync(ct);

            return Results.Ok(new { message = $"{comments.Count} comments approved" });
        })
        .WithName("BulkApproveComments")
        .WithSummary("Bulk approve comments (Admin only)")
        .RequireAuthorization()
        .Produces<object>(200)
        .Produces(400)
        .Produces(401)
        .Produces(403);

        // Bulk delete comments (authenticated - admin only)
        group.MapPost("/bulk-delete", [Authorize(Roles = "Admin")] async (
            BulkActionRequest request,
            BlogDbContext db,
            CancellationToken ct) =>
        {
            if (request.Ids == null || request.Ids.Count == 0)
                return Results.BadRequest(new { error = "No comment IDs provided" });

            var comments = await db.Comments
                .Where(c => request.Ids.Contains(c.Id))
                .ToListAsync(ct);

            db.Comments.RemoveRange(comments);
            await db.SaveChangesAsync(ct);

            return Results.Ok(new { message = $"{comments.Count} comments deleted" });
        })
        .WithName("BulkDeleteComments")
        .WithSummary("Bulk delete comments (Admin only)")
        .RequireAuthorization()
        .Produces<object>(200)
        .Produces(400)
        .Produces(401)
        .Produces(403);

        // Get comment statistics (authenticated - admin only)
        group.MapGet("/statistics", [Authorize(Roles = "Admin")] async (
            BlogDbContext db,
            CancellationToken ct) =>
        {
            var totalComments = await db.Comments.CountAsync(ct);
            var pendingComments = await db.Comments.CountAsync(c => c.Status == CommentStatus.Pending, ct);
            var approvedComments = await db.Comments.CountAsync(c => c.Status == CommentStatus.Approved, ct);
            var rejectedComments = await db.Comments.CountAsync(c => c.Status == CommentStatus.Rejected, ct);

            var today = DateTime.UtcNow.Date;
            var commentsToday = await db.Comments.CountAsync(c => c.CreatedAt >= today, ct);

            var commentsByPost = await db.Comments
                .GroupBy(c => new { c.PostId, c.Post.Title })
                .Select(g => new { g.Key.PostId, PostTitle = g.Key.Title, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(10)
                .ToListAsync(ct);

            return Results.Ok(new
            {
                total = totalComments,
                pending = pendingComments,
                approved = approvedComments,
                rejected = rejectedComments,
                today = commentsToday,
                topPosts = commentsByPost
            });
        })
        .WithName("GetCommentStatistics")
        .WithSummary("Get comment statistics (Admin only)")
        .RequireAuthorization()
        .Produces<object>(200)
        .Produces(401)
        .Produces(403);

        return app;
    }

    private static List<CommentNodeDto> BuildCommentTree(List<CommentFlatDto> flatComments)
    {
        var commentMap = flatComments.ToDictionary(c => c.Id, c => new CommentNodeDto
        {
            Id = c.Id,
            Content = c.Content,
            AuthorName = c.AuthorName,
            CreatedAt = c.CreatedAt,
            Children = new List<CommentNodeDto>()
        });

        var rootComments = new List<CommentNodeDto>();

        foreach (var comment in flatComments)
        {
            if (comment.ParentId.HasValue && commentMap.ContainsKey(comment.ParentId.Value))
            {
                // Add as child to parent
                commentMap[comment.ParentId.Value].Children.Add(commentMap[comment.Id]);
            }
            else
            {
                // This is a root comment
                rootComments.Add(commentMap[comment.Id]);
            }
        }

        return rootComments;
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }
}

// Request/Response DTOs
public class CreateCommentRequest
{
    public string Content { get; set; } = string.Empty;
    public string AuthorName { get; set; } = string.Empty;
    public string? AuthorEmail { get; set; }
    public int? ParentId { get; set; }
}

public class UpdateCommentRequest
{
    public string Content { get; set; } = string.Empty;
}

public class BulkActionRequest
{
    public List<int> Ids { get; set; } = new();
}

public class CommentDto
{
    public int Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public string AuthorName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public List<CommentDto> Replies { get; set; } = new();
}

public class CommentFlatDto
{
    public int Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public string AuthorName { get; set; } = string.Empty;
    public int? ParentId { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CommentNodeDto
{
    public int Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public string AuthorName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public List<CommentNodeDto> Children { get; set; } = new();
}

public class CommentDetailDto
{
    public int Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? AuthorName { get; set; }
    public string? AuthorEmail { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public int PostId { get; set; }
    public string PostTitle { get; set; } = string.Empty;
    public int? ParentId { get; set; }
}
