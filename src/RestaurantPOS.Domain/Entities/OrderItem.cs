using RestaurantPOS.Domain.Common;

namespace RestaurantPOS.Domain.Entities;

public class OrderItem : Entity
{
    public Guid OrderId { get; private set; }
    public Guid MenuItemId { get; private set; }
    public int Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public string? SpecialInstructions { get; private set; }
    public decimal Totalprice => UnitPrice * Quantity;

    public OrderItem(
        Guid orderId,
        Guid menuItemId,
        int quantity,
        decimal unitPrice,
        string? specialInstructions = null
    )
    {
        if (OrderId == Guid.Empty)
            throw new ArgumentException("Order is required. ");

        if (menuItemId == Guid.Empty)
            throw new ArgumentException("Menu item is required. ");

        if (quantity <= 0)
            throw new ArgumentException("Quantity must be greater than Zero. ");

        if (unitPrice < 0)
            throw new ArgumentException("Unit price cannot be negative. ");

        OrderId = orderId;
        MenuItemId = menuItemId;
        Quantity = quantity;
        UnitPrice = unitPrice;
        SpecialInstructions = string.IsNullOrWhiteSpace(specialInstructions)
            ? null
            : specialInstructions.Trim();
    }

    public void ChageQuantity(int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be greater than zero. ");

        Quantity = quantity;

        MarkUpdated();
    }

    public void ChangeSpecialInstructions(string? specialInstructions)
    {
        SpecialInstructions = string.IsNullOrWhiteSpace(specialInstructions)
            ? null
            : specialInstructions.Trim();

        MarkUpdated();
    }
}
