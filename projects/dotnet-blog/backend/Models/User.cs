using System.ComponentModel.DataAnnotations;

namespace DotnetBlog.Models;

public class User
{
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? DisplayName { get; set; }

    public string? Bio { get; set; }

    public string? Avatar { get; set; }

    public UserRole Role { get; set; } = UserRole.Reader;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? LastLoginAt { get; set; }

    public List<Post> Posts { get; set; } = [];
}

public enum UserRole
{
    Reader,
    Author,
    Admin
}
