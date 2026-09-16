using RestaurantPOS.Domain.Common;
using RestaurantPOS.Domain.Enums;

namespace RestaurantPOS.Domain.Entities;

public class Order : Entity
{
    public Guid TableSessionId { get; private set; }
    public Guid WaiterId { get; private set; }
    public OrderStatus Status { get; private set; }
    public DateTime? SubmittedAt { get; private set; }
    public DateTime? PreparingAt { get; private set; }
    public DateTime? ReadyAt { get; private set; }

    public Order(Guid tableSessionId, Guid waiterId)
    {
        if (tableSessionId == Guid.Empty)
            throw new ArgumentException("Table session is required. ");

        if (waiterId == Guid.Empty)
            throw new ArgumentException("Waiter is required");

        TableSessionId = tableSessionId;
        WaiterId = waiterId;

        Status = OrderStatus.Draft;
    }

    public void Submit()
    {
        if (Status != OrderStatus.Draft)
            throw new InvalidOperationException("Only a draft Order can be submitted.");

        Status = OrderStatus.Submitted;
        SubmittedAt = DateTime.UtcNow;

        MarkUpdated();
    }

    public void StartPreparing()
    {
        if (Status != OrderStatus.Submitted)
            throw new InvalidOperationException("Only a submitted order can start preparation.");

        Status = OrderStatus.Preparing;
        PreparingAt = DateTime.UtcNow;

        MarkUpdated();
    }

    public void MarkRead()
    {
        if (Status != OrderStatus.Preparing)
            throw new InvalidOperationException("Only an Order being Prepared can be marked read.");

        Status = OrderStatus.Ready;
        ReadyAt = DateTime.UtcNow;

        MarkUpdated();
    }
}
