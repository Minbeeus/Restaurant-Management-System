using Restaurant.Application.DTOs;

namespace Restaurant.Application.Interfaces;

public interface IIdentityService
{
    Task<LoginResponse?> LoginAsync(LoginRequest request);
    Task<LoginResponse?> QuickLoginAsync(QuickLoginRequest request);
    Task<UserDto?> GetUserByIdAsync(int userId);
    string HashPassword(string password);
    bool VerifyPassword(string password, string passwordHash);
}

public interface ICurrentUserService
{
    int? UserId { get; }
    string? Username { get; }
    string? RoleName { get; }
    bool IsAuthenticated { get; }
}
