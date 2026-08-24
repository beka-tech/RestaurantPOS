using RestaurantPOS.Domain.Common;

namespace RestaurantPOS.Domain.Entities;

public class OrderItemModifier : Entity
{
    public Guid OrderItemId { get; private set; }
    public Guid ModifierId { get; private set; }
    public string Name { get; private set; }
    public decimal AdditionalPrice { get; private set; }

    public OrderItemModifier(
        Guid orderItemId,
        Guid modifierId,
        string name,
        decimal additionalPrice
    )
    {
        if (orderItemId == Guid.Empty)
            throw new ArgumentException("Order item is required. ");

        if (modifierId == Guid.Empty)
            throw new ArgumentException("Modifier is required. ");

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Modifier name is required. ");

        if (additionalPrice < 0)
            throw new ArgumentException("Additional price cannot be negative . ");

        OrderItemId = orderItemId;
        ModifierId = modifierId;
        Name = name.Trim();
        AdditionalPrice = additionalPrice;
    }
}
