using System.ComponentModel.DataAnnotations;

namespace DotnetBlog.Models;

public class Post
{
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Slug { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public string? Excerpt { get; set; }

    public string? CoverImage { get; set; }

    public PostStatus Status { get; set; } = PostStatus.Draft;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? PublishedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public int ViewCount { get; set; }

    public int AuthorId { get; set; }
    public User Author { get; set; } = null!;

    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;

    public List<Tag> Tags { get; set; } = [];

    public List<Comment> Comments { get; set; } = [];
}

public enum PostStatus
{
    Draft,
    Published,
    Archived
}
