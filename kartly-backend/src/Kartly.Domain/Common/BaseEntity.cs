namespace Kartly.Domain.Common;

// -----------------------------------------------------------------------
// Every entity shares an Id and audit timestamps. Keeping this in one
// base class avoids repeating the same three properties on every model
// and gives us one place to add soft-delete or concurrency tokens later.
// -----------------------------------------------------------------------
public abstract class BaseEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
