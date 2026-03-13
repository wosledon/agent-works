using DotnetBlog.Data;
using DotnetBlog.Models;
using Microsoft.EntityFrameworkCore;

namespace DotnetBlog.Services;

public interface IAuthService
{
    Task<AuthResult> RegisterAsync(RegisterRequest request);
    Task<AuthResult> LoginAsync(LoginRequest request);
    Task<AuthResult> RefreshTokenAsync(RefreshTokenRequest request);
    Task<User?> GetUserByIdAsync(int id);
}

public class AuthService : IAuthService
{
    private readonly BlogDbContext _db;
    private readonly IJwtService _jwtService;

    public AuthService(BlogDbContext db, IJwtService jwtService)
    {
        _db = db;
        _jwtService = jwtService;
    }

    public async Task<AuthResult> RegisterAsync(RegisterRequest request)
    {
        // Check if username already exists
        if (await _db.Users.AnyAsync(u => u.Username == request.Username))
        {
            return AuthResult.Failure("Username already taken");
        }

        // Check if email already exists
        if (await _db.Users.AnyAsync(u => u.Email == request.Email))
        {
            return AuthResult.Failure("Email already registered");
        }

        // Hash password using BCrypt
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

        var user = new User
        {
            Username = request.Username,
            Email = request.Email,
            PasswordHash = passwordHash,
            DisplayName = request.DisplayName ?? request.Username,
            Role = UserRole.Reader,
            CreatedAt = DateTime.UtcNow
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        var tokens = _jwtService.GenerateTokens(user);

        return AuthResult.Success(
            new UserResponse(
                Id: user.Id,
                Username: user.Username,
                Email: user.Email,
                DisplayName: user.DisplayName,
                Bio: user.Bio,
                Avatar: user.Avatar,
                Role: user.Role.ToString(),
                CreatedAt: user.CreatedAt
            ),
            tokens
        );
    }

    public async Task<AuthResult> LoginAsync(LoginRequest request)
    {
        // Find user by username or email
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Username == request.Login || u.Email == request.Login);

        if (user is null)
        {
            return AuthResult.Failure("Invalid credentials");
        }

        // Verify password
        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            return AuthResult.Failure("Invalid credentials");
        }

        // Update last login
        user.LastLoginAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        var tokens = _jwtService.GenerateTokens(user);

        return AuthResult.Success(
            new UserResponse(
                Id: user.Id,
                Username: user.Username,
                Email: user.Email,
                DisplayName: user.DisplayName,
                Bio: user.Bio,
                Avatar: user.Avatar,
                Role: user.Role.ToString(),
                CreatedAt: user.CreatedAt,
                LastLoginAt: user.LastLoginAt
            ),
            tokens
        );
    }

    public async Task<AuthResult> RefreshTokenAsync(RefreshTokenRequest request)
    {
        // Validate refresh token
        var principal = _jwtService.ValidateRefreshToken(request.RefreshToken);
        if (principal is null)
        {
            return AuthResult.Failure("Invalid refresh token");
        }

        // Get user ID from token
        var userIdClaim = principal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (userIdClaim is null || !int.TryParse(userIdClaim.Value, out var userId))
        {
            return AuthResult.Failure("Invalid token");
        }

        // Find user
        var user = await _db.Users.FindAsync(userId);
        if (user is null)
        {
            return AuthResult.Failure("User not found");
        }

        // Generate new tokens
        var tokens = _jwtService.GenerateTokens(user);

        return AuthResult.Success(
            new UserResponse(
                Id: user.Id,
                Username: user.Username,
                Email: user.Email,
                DisplayName: user.DisplayName,
                Bio: user.Bio,
                Avatar: user.Avatar,
                Role: user.Role.ToString(),
                CreatedAt: user.CreatedAt,
                LastLoginAt: user.LastLoginAt
            ),
            tokens
        );
    }

    public async Task<User?> GetUserByIdAsync(int id)
    {
        return await _db.Users.FindAsync(id);
    }
}

public record RegisterRequest(
    string Username,
    string Email,
    string Password,
    string? DisplayName = null
);

public record LoginRequest(
    string Login,  // Can be username or email
    string Password
);

public record RefreshTokenRequest(
    string RefreshToken
);

public record UserResponse(
    int Id,
    string Username,
    string Email,
    string? DisplayName,
    string? Bio,
    string? Avatar,
    string Role,
    DateTime CreatedAt,
    DateTime? LastLoginAt = null
);

public class AuthResult
{
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public UserResponse? User { get; set; }
    public TokenResponse? Tokens { get; set; }

    public static AuthResult Success(UserResponse user, TokenResponse tokens)
    {
        return new AuthResult { IsSuccess = true, User = user, Tokens = tokens };
    }

    public static AuthResult Failure(string errorMessage)
    {
        return new AuthResult { IsSuccess = false, ErrorMessage = errorMessage };
    }
}
