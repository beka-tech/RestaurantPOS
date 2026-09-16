using RestaurantPOS.Domain.Common;

namespace RestaurantPOS.Domain.Entities;

public class Modifier : Entity
{
    public string Name { get; private set; }
    public decimal AdditionalPrice { get; private set; }

    public bool IsActive { get; private set; }

    public Modifier(string name, decimal additionalPrice)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Modfier name is requird. ");

        if (additionalPrice < 0)
            throw new ArgumentException("Additional price cannot be negative. ");

        Name = name.Trim();
        AdditionalPrice = additionalPrice;
        IsActive = true;
    }

    public void ChangeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Modifier name is required. ");

        Name = name.Trim();

        MarkUpdated();
    }

    public void ChangeAdditionalPrice(decimal additionalPrice)
    {
        if (additionalPrice < 0)
            throw new ArgumentException("Additional price cannot be negative. ");

        AdditionalPrice = additionalPrice;
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
