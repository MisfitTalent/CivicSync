using Volo.Abp.Domain.Entities;

namespace CivicSync.Node.Api.Domain.Common;

public abstract class EntityBase : Entity<Guid>
{
    protected EntityBase()
    {
        Id = Guid.NewGuid();
    }

    protected EntityBase(Guid id)
    {
        Id = id;
    }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }

    protected void MarkUpdated()
    {
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
