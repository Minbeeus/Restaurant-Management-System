using Restaurant.Domain.Common;

namespace Restaurant.Domain.Entities;

public class Register : AuditableEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public ICollection<Shift> Shifts { get; set; } = new List<Shift>();
}
