using Microsoft.EntityFrameworkCore;
using Restaurant.Application.Common;
using Restaurant.Application.Interfaces;
using Restaurant.Infrastructure.Persistence;

namespace Restaurant.Infrastructure.Services;

public class PermissionService : IPermissionService
{
    private readonly ApplicationDbContext _context;

    public PermissionService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> HasPermissionAsync(int userId, string permission)
    {
        var permissions = await GetUserPermissionsAsync(userId);
        return permissions.Contains(permission);
    }

    public async Task<HashSet<string>> GetUserPermissionsAsync(int userId)
    {
        var user = await _context.Users
            .AsNoTracking()
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null || user.Role == null)
        {
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        return Permissions.GetPermissionsForRole(user.Role.Name);
    }

    public async Task EnsurePermissionAsync(int userId, string permission)
    {
        var hasPerm = await HasPermissionAsync(userId, permission);
        if (!hasPerm)
        {
            throw new UnauthorizedAccessException($"Tài khoản không có quyền thực hiện thao tác '{permission}'.");
        }
    }
}
