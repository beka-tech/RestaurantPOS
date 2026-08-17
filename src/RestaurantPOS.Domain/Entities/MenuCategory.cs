using RestaurantPOS.Domain.Common;

namespace RestaurantPOS.Domain.Entities;

public class MenuCategory : Entity
{
    public string Name { get; private set; }
    public int DisplayOrder { get; private set; }
    public bool IsActive { get; private set; }

    public MenuCategory(string name, int displayOrder)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Category name is required. ");

        if (displayOrder < 0)
            throw new ArgumentException("DisPlay Order cannot be Negative");

        Name = name.Trim();
        DisplayOrder = displayOrder;
        IsActive = true;
    }

    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Category name is required ");

        Name = name.Trim();

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

        MarkUpdated();
    }
}
