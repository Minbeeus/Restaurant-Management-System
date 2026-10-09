using Restaurant.Application.Interfaces;
using Restaurant.Domain.Entities;
using Restaurant.Infrastructure.Persistence;

namespace Restaurant.Infrastructure.Services;

public class AuditService : IAuditService
{
    private readonly ApplicationDbContext _context;

    public AuditService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task LogAsync(int userId, string action, string entityName, string? entityId, string details, int? registerId = null, int? shiftId = null, string? correlationId = null)
    {
        if (userId <= 0)
        {
            var defaultUser = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(_context.Users.OrderBy(u => u.Id));
            if (defaultUser != null)
            {
                userId = defaultUser.Id;
            }
            else
            {
                userId = 1;
            }
        }

        var entry = new AuditLogEntry
        {
            UserId = userId,
            Action = action,
            EntityName = entityName,
            EntityId = entityId,
            Details = details,
            RegisterId = registerId,
            ShiftId = shiftId,
            CorrelationId = correlationId ?? Guid.NewGuid().ToString("N"),
            CreatedAt = DateTime.UtcNow
        };

        _context.AuditLogEntries.Add(entry);
        await _context.SaveChangesAsync();
    }
}
