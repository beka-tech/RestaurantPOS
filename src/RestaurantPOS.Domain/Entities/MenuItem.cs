using RestaurantPOS.Domain.Common;

namespace RestaurantPOS.Domain.Entities;

public class MenuItem : Entity
{
    public Guid MenuCategoryId { get; private set; }

    public string Name { get; private set; }

    public string? Description { get; private set; }

    public decimal Price { get; private set; }

    public bool IsAvailable { get; private set; }

    public bool IsActive { get; private set; }

    public MenuItem(Guid menuCategoryId, string name, string? description, decimal price)
    {
        if (menuCategoryId == Guid.Empty)
            throw new ArgumentException("Menu category is required.");

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Menu item name is required.");

        if (price < 0)
            throw new ArgumentException("Price cannot be negative.");

        MenuCategoryId = menuCategoryId;
        Name = name.Trim();
        Description = description?.Trim();
        Price = price;

        IsAvailable = true;
        IsActive = true;
    }

    public void UpdateDetails(string name, string? description)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Menu item name is required.");

        Name = name.Trim();
        Description = description?.Trim();

        MarkUpdated();
    }

    public void ChangePrice(decimal price)
    {
        if (price < 0)
            throw new ArgumentException("Price cannot be negative.");

        Price = price;

        MarkUpdated();
    }

    public void MakeAvailable()
    {
        if (!IsActive)
            throw new InvalidOperationException("Inactive menu item cannot be made available.");

        IsAvailable = true;

        MarkUpdated();
    }

    public void MakeUnavailable()
    {
        IsAvailable = false;

        MarkUpdated();
    }

    public void Activate()
    {
        IsActive = true;

        MarkUpdated();
    }

    public void Deactivate()
    {
        IsActive = false;
        IsAvailable = false;

        MarkUpdated();
    }
}
