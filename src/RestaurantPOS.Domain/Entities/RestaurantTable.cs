using RestaurantPOS.Domain.Common;
using RestaurantPOS.Domain.Enums;

namespace RestaurantPOS.Domain.Entities;

public class RestaurantTable : Entity
{
    public string Number { get; private set; }

    public int Capacity { get; private set; }

    public TableStatus Status { get; private set; }

    public bool IsActive { get; private set; }

    public RestaurantTable(string number, int capacity)
    {
        if (string.IsNullOrWhiteSpace(number))
            throw new ArgumentException("Table number is required.");

        if (capacity <= 0)
            throw new ArgumentException("Table capacity must be greater than zero.");

        Number = number.Trim();
        Capacity = capacity;

        Status = TableStatus.Available;
        IsActive = true;
    }

    public void ChangeNumber(string number)
    {
        if (string.IsNullOrWhiteSpace(number))
            throw new ArgumentException("Table number is required.");

        Number = number.Trim();

        MarkUpdated();
    }

    public void ChangeCapacity(int capacity)
    {
        if (Status != TableStatus.Available)
            throw new InvalidOperationException(
                "Capacity cannot be chnaged while the table is occupied"
            );

        if (capacity <= 0)
            throw new ArgumentException("Table capacity must be greater than zero.");

        Capacity = capacity;

        MarkUpdated();
    }

    public void Occupy()
    {
        if (!IsActive)
            throw new InvalidOperationException("Inactive table cannot be occupied.");

        if (Status != TableStatus.Available)
            throw new InvalidOperationException("Only an available table can be occupied.");

        Status = TableStatus.Occupied;

        MarkUpdated();
    }

    public void MakeAvailable()
    {
        if (Status != TableStatus.Occupied)
            throw new InvalidOperationException("Only an occupied table can be made available.");

        Status = TableStatus.Available;

        MarkUpdated();
    }

    public void Activate()
    {
        if (IsActive)
            throw new InvalidOperationException("Table is already active.");

        IsActive = true;

        MarkUpdated();
    }

    public void Deactivate()
    {
        if (!IsActive)
            throw new InvalidOperationException("Table is already inactive.");

        if (Status != TableStatus.Available)
            throw new InvalidOperationException("Only an available table can be deactivated.");

        IsActive = false;

        MarkUpdated();
    }
}
