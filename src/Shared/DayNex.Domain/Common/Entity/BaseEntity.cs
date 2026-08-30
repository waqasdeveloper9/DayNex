using DayNex.Domain.Common.Interface;

namespace DayNex.Domain.Common.Entity
{
    public abstract class BaseEntity : IEntity
    {
        public Guid Id { get; protected set; } = Guid.NewGuid();
        public DateTime CreatedAtUtc { get; protected set; } = DateTime.UtcNow;
        public DateTime? UpdatedAtUtc { get; protected set; }
        public bool IsDeleted { get; protected set; }

        public void MarkUpdated() => UpdatedAtUtc = DateTime.UtcNow;

        public void SoftDelete()
        {
            IsDeleted = true;
            MarkUpdated();
        }
    }
}
