using RestaurantPOS.Domain.Common;

namespace RestaurantPOS.Domain.Entities;

public class MenuCategory : Entity
{
    public string Name { get; private set; }
    public string? Description { get; private set; }
    public int DisplayOrder { get; private set; }
    public bool IsActive { get; private set; }

    public MenuCategory(string name, int displayOrder, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentNullException("Category name is requird . ");

        if (displayOrder < 0)
            throw new ArgumentException("Display order cannot be negative. ");

        Name = name.Trim();
        DisplayOrder = displayOrder;

        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();

        IsActive = true;
    }

    public void ChangeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Category name is requird. ");
    }

    public void ChangeDescription(string? description)
    {
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        MarkUpdated();
    }

    public void ChangeDisplayOrder(int displayOrder)
    {
        if (displayOrder < 0)
            throw new ArgumentException("Display order cannot be negative . ");

        DisplayOrder = displayOrder;

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
