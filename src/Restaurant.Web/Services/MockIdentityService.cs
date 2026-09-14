using Restaurant.Application.DTOs;
using Restaurant.Application.Interfaces;

namespace Restaurant.Web.Services;

public class MockIdentityService : IIdentityService
{
    public Task<LoginResponse?> LoginAsync(LoginRequest request)
    {
        return Task.FromResult<LoginResponse?>(new LoginResponse
        {
            Token = "mock-token",
            Username = request.Username,
            RoleName = "Admin",
            FullName = "Mock Admin"
        });
    }

    public Task<LoginResponse?> QuickLoginAsync(QuickLoginRequest request)
    {
        // Mock PIN validation for testing the UI
        if (request.AccessCode == "123456")
        {
            return Task.FromResult<LoginResponse?>(new LoginResponse
            {
                Token = "mock-token",
                Username = "test_chef",
                RoleName = "Chef",
                FullName = "Đầu bếp Test"
            });
        }
        
        return Task.FromResult<LoginResponse?>(null);
    }

    public Task<UserDto?> GetUserByIdAsync(int userId)
    {
        return Task.FromResult<UserDto?>(new UserDto { Id = userId, Username = "test", FullName = "Test" });
    }

    public string HashPassword(string password)
    {
        return password; // Mock implementation
    }

    public bool VerifyPassword(string password, string passwordHash)
    {
        return true; // Mock implementation
    }
}
