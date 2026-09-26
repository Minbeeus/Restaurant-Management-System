namespace Restaurant.Application.Interfaces;

public interface IAuditService
{
    Task LogAsync(int userId, string action, string entityName, string? entityId, string details, int? registerId = null, int? shiftId = null, string? correlationId = null);
}
