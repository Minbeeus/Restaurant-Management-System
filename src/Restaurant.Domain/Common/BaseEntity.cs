namespace Restaurant.Domain.Common;

public abstract class BaseEntity
{
    public int Id { get; set; }
}

public abstract class AuditableEntity : BaseEntity
{
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public abstract class FullAuditableEntity : AuditableEntity
{
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
}

public interface IHasRowVersion
{
    byte[] RowVersion { get; set; }
}
