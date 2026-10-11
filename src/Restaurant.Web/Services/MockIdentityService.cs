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

    public Task<PinLoginResponse?> PinLoginAsync(PinLoginRequest request)
    {
        if (request.PinCode == "123456")
        {
            return Task.FromResult<PinLoginResponse?>(new PinLoginResponse
            {
                Token = "mock-token",
                Username = "test_user",
                RoleName = "Cashier",
                FullName = "Thu Ngân Test",
                DestinationArea = "POS"
            });
        }
        return Task.FromResult<PinLoginResponse?>(null);
    }

    public Task<bool> VerifyManagerPinAsync(string pinCode)
    {
        return Task.FromResult(pinCode == "123456");
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

    public Task<List<UserDto>> GetAllUsersAsync()
    {
        return Task.FromResult(new List<UserDto>
        {
            new UserDto { Id = 1, Username = "admin", FullName = "Quản trị viên Hệ thống", RoleName = "Admin", PhoneNumber = "0912345678", IsActive = true },
            new UserDto { Id = 2, Username = "manager01", FullName = "Nguyễn Hoàng Nam", RoleName = "Manager", PhoneNumber = "0934567890", IsActive = true },
            new UserDto { Id = 3, Username = "cashier01", FullName = "Lê Thị Thu", RoleName = "Cashier", PhoneNumber = "0945678901", IsActive = true },
            new UserDto { Id = 4, Username = "chef01", FullName = "Phạm Quốc Bảo", RoleName = "Chef", PhoneNumber = "0956789012", IsActive = true }
        });
    }

    public Task<UserDto> CreateUserAsync(CreateUserRequest request)
    {
        return Task.FromResult(new UserDto
        {
            Id = 99,
            Username = request.Username,
            FullName = request.FullName,
            RoleName = "Staff",
            Email = request.Email,
            PhoneNumber = request.PhoneNumber,
            IsActive = true
        });
    }

    public Task<bool> ToggleUserStatusAsync(int id)
    {
        return Task.FromResult(true);
    }
}
