using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Restaurant.Application.DTOs;
using Restaurant.Application.Interfaces;
using Restaurant.Domain.Entities;
using Restaurant.Infrastructure.Persistence;

namespace Restaurant.Infrastructure.Services;

public class IdentityService : IIdentityService
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;

    public IdentityService(ApplicationDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    public async Task<LoginResponse?> LoginAsync(LoginRequest request)
    {
        var user = await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Username == request.Username && u.IsActive);

        if (user == null || !VerifyPassword(request.Password, user.PasswordHash))
        {
            return null;
        }

        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_configuration["JwtSettings:SecretKey"] ?? "RestaurantManagementSecretKeySuperProtection123!");
        var expiryMinutes = double.Parse(_configuration["JwtSettings:ExpiryMinutes"] ?? "480");
        var expiresAt = DateTime.UtcNow.AddMinutes(expiryMinutes);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.GivenName, user.FullName),
                new Claim(ClaimTypes.Role, user.Role.Name)
            }),
            Expires = expiresAt,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);

        return new LoginResponse
        {
            Token = tokenHandler.WriteToken(token),
            Username = user.Username,
            FullName = user.FullName,
            RoleName = user.Role.Name,
            ExpiresAt = expiresAt
        };
    }

    public async Task<LoginResponse?> QuickLoginAsync(QuickLoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.AccessCode) || request.AccessCode.Length != 6)
        {
            return null;
        }

        var employeeCode = request.AccessCode;
        var employeeCodeWithPrefix = "NV" + employeeCode;

        var user = await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => (u.EmployeeCode == employeeCode || u.EmployeeCode == employeeCodeWithPrefix) && u.IsActive);

        if (user == null)
        {
            return null;
        }

        // Only allow Quick Login for Chef and Cashier
        if (user.Role.Name != "Chef" && user.Role.Name != "Cashier")
        {
            return null;
        }

        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_configuration["JwtSettings:SecretKey"] ?? "RestaurantManagementSecretKeySuperProtection123!");
        var expiryMinutes = double.Parse(_configuration["JwtSettings:ExpiryMinutes"] ?? "480");
        var expiresAt = DateTime.UtcNow.AddMinutes(expiryMinutes);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.GivenName, user.FullName),
                new Claim(ClaimTypes.Role, user.Role.Name)
            }),
            Expires = expiresAt,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);

        return new LoginResponse
        {
            Token = tokenHandler.WriteToken(token),
            Username = user.Username,
            FullName = user.FullName,
            RoleName = user.Role.Name,
            ExpiresAt = expiresAt
        };
    }

    public async Task<UserDto?> GetUserByIdAsync(int userId)
    {
        var user = await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null) return null;

        return new UserDto
        {
            Id = user.Id,
            Username = user.Username,
            FullName = user.FullName,
            RoleName = user.Role.Name,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            IsActive = user.IsActive
        };
    }

    public string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password);
    }

    public bool VerifyPassword(string password, string passwordHash)
    {
        return BCrypt.Net.BCrypt.Verify(password, passwordHash);
    }
}
