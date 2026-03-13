using System.ComponentModel.DataAnnotations;

namespace DotnetBlog.Models;

public class Comment
{
    public int Id { get; set; }

    [Required]
    public string Content { get; set; } = string.Empty;

    public string? AuthorName { get; set; }

    public string? AuthorEmail { get; set; }

    public CommentStatus Status { get; set; } = CommentStatus.Pending;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public int PostId { get; set; }
    public Post Post { get; set; } = null!;

    public int? ParentId { get; set; }
    public Comment? Parent { get; set; }

    public List<Comment> Replies { get; set; } = [];
}

public enum CommentStatus
{
    Pending,
    Approved,
    Rejected
}
