namespace RestaurantPOS.Domain.Common;

public abstract class Entity
{
    public Guid ID { get; protected set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; protected set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; protected set; }

    protected void MarkUpdated()
    {
        UpdatedAt = DateTime.UtcNow;
    }
}
