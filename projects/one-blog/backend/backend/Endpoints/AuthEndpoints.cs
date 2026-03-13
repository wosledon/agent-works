using DotnetBlog.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DotnetBlog.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth")
            .WithTags("Authentication");

        // Register
        group.MapPost("/register", async (RegisterRequest request, IAuthService authService) =>
        {
            var result = await authService.RegisterAsync(request);

            if (!result.IsSuccess)
            {
                return Results.BadRequest(new { error = result.ErrorMessage });
            }

            return Results.Ok(new
            {
                user = result.User,
                tokens = result.Tokens
            });
        })
        .WithName("Register")
        .WithSummary("Register a new user")
        .Produces<object>(200)
        .Produces<object>(400);

        // Login
        group.MapPost("/login", async (LoginRequest request, IAuthService authService) =>
        {
            var result = await authService.LoginAsync(request);

            if (!result.IsSuccess)
            {
                return Results.Unauthorized();
            }

            return Results.Ok(new
            {
                user = result.User,
                tokens = result.Tokens
            });
        })
        .WithName("Login")
        .WithSummary("Authenticate user and get tokens")
        .Produces<object>(200)
        .Produces(401);

        // Refresh Token
        group.MapPost("/refresh", async (RefreshTokenRequest request, IAuthService authService) =>
        {
            var result = await authService.RefreshTokenAsync(request);

            if (!result.IsSuccess)
            {
                return Results.Unauthorized();
            }

            return Results.Ok(new
            {
                user = result.User,
                tokens = result.Tokens
            });
        })
        .WithName("RefreshToken")
        .WithSummary("Refresh access token using refresh token")
        .Produces<object>(200)
        .Produces(401);

        // Get Current User
        group.MapGet("/me", [Authorize] async (IAuthService authService, ClaimsPrincipal user) =>
        {
            var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId is null || !int.TryParse(userId, out var id))
            {
                return Results.Unauthorized();
            }

            var currentUser = await authService.GetUserByIdAsync(id);
            if (currentUser is null)
            {
                return Results.NotFound();
            }

            return Results.Ok(new UserResponse(
                Id: currentUser.Id,
                Username: currentUser.Username,
                Email: currentUser.Email,
                DisplayName: currentUser.DisplayName,
                Bio: currentUser.Bio,
                Avatar: currentUser.Avatar,
                Role: currentUser.Role.ToString(),
                CreatedAt: currentUser.CreatedAt,
                LastLoginAt: currentUser.LastLoginAt
            ));
        })
        .WithName("GetCurrentUser")
        .WithSummary("Get current authenticated user")
        .Produces<UserResponse>(200)
        .Produces(401)
        .Produces(404);

        return app;
    }
}
