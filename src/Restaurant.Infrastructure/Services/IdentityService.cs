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

    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, (int FailedCount, DateTime LockoutUntil)> _pinLoginAttempts = new();

    public async Task<PinLoginResponse?> PinLoginAsync(PinLoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.PinCode))
        {
            return null;
        }

        var pinCode = request.PinCode.Trim();
        var clientKey = "PIN_LOCKOUT_" + pinCode;
        if (_pinLoginAttempts.TryGetValue(clientKey, out var attemptInfo))
        {
            if (attemptInfo.LockoutUntil > DateTime.UtcNow)
            {
                var remaining = (int)Math.Ceiling((attemptInfo.LockoutUntil - DateTime.UtcNow).TotalMinutes);
                throw new InvalidOperationException($"Mã PIN bị khóa do nhập sai 5 lần. Vui lòng thử lại sau {remaining} phút.");
            }
        }
        var employeeCodeWithPrefix = "NV" + pinCode;

        var activeUsers = await _context.Users
            .Include(u => u.Role)
            .Where(u => u.IsActive)
            .ToListAsync();

        User? matchedUser = null;
        foreach (var u in activeUsers)
        {
            if (!string.IsNullOrEmpty(u.PinCodeHash) && VerifyPassword(pinCode, u.PinCodeHash))
            {
                matchedUser = u;
                break;
            }
            if (u.EmployeeCode == pinCode || u.EmployeeCode == employeeCodeWithPrefix)
            {
                matchedUser = u;
                break;
            }
        }

        if (matchedUser == null)
        {
            var currentCount = attemptInfo.FailedCount + 1;
            if (currentCount >= 5)
            {
                _pinLoginAttempts[clientKey] = (currentCount, DateTime.UtcNow.AddMinutes(15));
                throw new InvalidOperationException("Mã PIN bị khóa do nhập sai 5 lần. Vui lòng thử lại sau 15 phút.");
            }
            else
            {
                _pinLoginAttempts[clientKey] = (currentCount, DateTime.MinValue);
            }
            return null;
        }

        // Reset rate limiter on success
        _pinLoginAttempts.TryRemove(clientKey, out _);

        var permissions = Application.Common.Permissions.GetPermissionsForRole(matchedUser.Role.Name);
        var hasPos = permissions.Contains(Application.Common.Permissions.PosAccess);
        var hasKds = permissions.Contains(Application.Common.Permissions.KdsAccess);

        string destinationArea;
        if (hasPos && hasKds)
        {
            destinationArea = "BOTH";
        }
        else if (hasPos)
        {
            destinationArea = "POS";
        }
        else if (hasKds)
        {
            destinationArea = "KDS";
        }
        else
        {
            destinationArea = "ADMIN";
        }

        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_configuration["JwtSettings:SecretKey"] ?? "RestaurantManagementSecretKeySuperProtection123!");
        var expiryMinutes = double.Parse(_configuration["JwtSettings:ExpiryMinutes"] ?? "480");
        var expiresAt = DateTime.UtcNow.AddMinutes(expiryMinutes);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, matchedUser.Id.ToString()),
            new Claim(ClaimTypes.Name, matchedUser.Username),
            new Claim(ClaimTypes.GivenName, matchedUser.FullName),
            new Claim(ClaimTypes.Role, matchedUser.Role.Name)
        };

        if (hasPos) claims.Add(new Claim("Permission", Application.Common.Permissions.PosAccess));
        if (hasKds) claims.Add(new Claim("Permission", Application.Common.Permissions.KdsAccess));

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = expiresAt,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);

        return new PinLoginResponse
        {
            Token = tokenHandler.WriteToken(token),
            Username = matchedUser.Username,
            FullName = matchedUser.FullName,
            RoleName = matchedUser.Role.Name,
            Permissions = permissions.ToList(),
            DestinationArea = destinationArea,
            ExpiresAt = expiresAt
        };
    }

    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, (int FailedCount, DateTime LockoutUntil)> _managerPinAttempts = new();

    public static void ResetManagerPinAttempts() => _managerPinAttempts.Clear();

    public async Task<bool> VerifyManagerPinAsync(string pinCode)
    {
        if (string.IsNullOrWhiteSpace(pinCode)) return false;
        var trimmedPin = pinCode.Trim();

        const string lockoutKey = "GLOBAL_MGR_PIN_LOCKOUT";
        if (_managerPinAttempts.TryGetValue(lockoutKey, out var attemptInfo))
        {
            if (attemptInfo.LockoutUntil > DateTime.UtcNow)
            {
                var remaining = (int)Math.Ceiling((attemptInfo.LockoutUntil - DateTime.UtcNow).TotalMinutes);
                if (remaining <= 0) remaining = 1;
                throw new InvalidOperationException($"Mã PIN Quản lý bị khóa do nhập sai 5 lần. Vui lòng thử lại sau {remaining} phút.");
            }
            else if (attemptInfo.LockoutUntil > DateTime.MinValue)
            {
                // Previous lockout expired; reset attempts
                _managerPinAttempts.TryRemove(lockoutKey, out _);
                attemptInfo = (0, DateTime.MinValue);
            }
        }

        var activeManagersAndAdmins = await _context.Users
            .Include(u => u.Role)
            .Where(u => u.IsActive && (u.Role.Name == "Admin" || u.Role.Name == "Manager"))
            .ToListAsync();

        User? matchedUser = null;
        foreach (var u in activeManagersAndAdmins)
        {
            if (!string.IsNullOrEmpty(u.PinCodeHash) && VerifyPassword(trimmedPin, u.PinCodeHash))
            {
                matchedUser = u;
                break;
            }
        }

        if (matchedUser == null)
        {
            var currentCount = attemptInfo.FailedCount + 1;
            if (currentCount >= 5)
            {
                _managerPinAttempts[lockoutKey] = (currentCount, DateTime.UtcNow.AddMinutes(15));
                throw new InvalidOperationException("Mã PIN Quản lý bị khóa do nhập sai 5 lần. Vui lòng thử lại sau 15 phút.");
            }
            else
            {
                _managerPinAttempts[lockoutKey] = (currentCount, DateTime.MinValue);
            }
            return false;
        }

        // Reset rate limiter on success
        _managerPinAttempts.TryRemove(lockoutKey, out _);
        return true;
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
            RoleName = user.Role?.Name ?? "Staff",
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            IsActive = user.IsActive
        };
    }

    public async Task<List<UserDto>> GetAllUsersAsync()
    {
        var users = await _context.Users
            .AsNoTracking()
            .Include(u => u.Role)
            .Where(u => !u.IsDeleted)
            .OrderBy(u => u.Id)
            .ToListAsync();

        return users.Select(u => new UserDto
        {
            Id = u.Id,
            Username = u.Username,
            FullName = u.FullName,
            RoleName = u.Role?.Name ?? "Staff",
            Email = u.Email,
            PhoneNumber = u.PhoneNumber,
            IsActive = u.IsActive
        }).ToList();
    }

    public async Task<UserDto> CreateUserAsync(CreateUserRequest request)
    {
        var existing = await _context.Users.AnyAsync(u => u.Username.ToLower() == request.Username.Trim().ToLower());
        if (existing)
        {
            throw new InvalidOperationException($"Tên đăng nhập '{request.Username}' đã tồn tại trong hệ thống.");
        }

        var roleId = request.RoleId > 0 ? request.RoleId : 3; // Default to Cashier if not specified
        var role = await _context.Roles.FindAsync(roleId);

        var newUser = new User
        {
            Username = request.Username.Trim(),
            PasswordHash = HashPassword(!string.IsNullOrWhiteSpace(request.Password) ? request.Password : "123456"),
            FullName = request.FullName.Trim(),
            RoleId = roleId,
            EmployeeCode = request.EmployeeCode?.Trim(),
            PhoneNumber = request.PhoneNumber?.Trim(),
            Email = request.Email?.Trim(),
            PinCodeHash = !string.IsNullOrWhiteSpace(request.PinCode) ? BCrypt.Net.BCrypt.HashPassword(request.PinCode.Trim()) : null,
            IsActive = true
        };

        _context.Users.Add(newUser);
        await _context.SaveChangesAsync();

        return new UserDto
        {
            Id = newUser.Id,
            Username = newUser.Username,
            FullName = newUser.FullName,
            RoleName = role?.Name ?? "Staff",
            Email = newUser.Email,
            PhoneNumber = newUser.PhoneNumber,
            IsActive = newUser.IsActive
        };
    }

    public async Task<bool> ToggleUserStatusAsync(int userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return false;

        user.IsActive = !user.IsActive;
        await _context.SaveChangesAsync();
        return true;
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
