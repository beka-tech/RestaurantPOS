using RestaurantPOS.Domain.Common;
using RestaurantPOS.Domain.Enums;

namespace RestaurantPOS.Domain.Entities;

public class TableSession : Entity
{
    public Guid RestaurantTableId { get; private set; }

    public Guid WaiterId { get; private set; }

    public TableSessionStatus Status { get; private set; }

    public int GuestCount { get; private set; }

    public DateTime OpenedAt { get; private set; }

    public DateTime? BillRequestedAt { get; private set; }

    public DateTime? ClosedAt { get; private set; }

    public TableSession(Guid restaurantTableId, Guid waiterId, int guestCount)
    {
        if (restaurantTableId == Guid.Empty)
            throw new ArgumentException("Restaurant table is required.");

        if (waiterId == Guid.Empty)
            throw new ArgumentException("Waiter is required.");

        if (guestCount <= 0)
            throw new ArgumentException("Guest count must be greater than zero.");

        RestaurantTableId = restaurantTableId;
        WaiterId = waiterId;
        GuestCount = guestCount;

        Status = TableSessionStatus.Open;
        OpenedAt = DateTime.UtcNow;
    }

    public void ChangeGuestCount(int guestCount)
    {
        if (Status != TableSessionStatus.Open)
            throw new InvalidOperationException(
                "Guest count can only be changed for an open session."
            );

        if (guestCount <= 0)
            throw new ArgumentException("Guest count must be greater than zero.");

        GuestCount = guestCount;

        MarkUpdated();
    }

    public void RequestBill()
    {
        if (Status != TableSessionStatus.Open)
            throw new InvalidOperationException("Bill can only be requested for an open session.");

        Status = TableSessionStatus.BillRequested;
        BillRequestedAt = DateTime.UtcNow;

        MarkUpdated();
    }

    public void MarkPaymentPending()
    {
        if (Status != TableSessionStatus.BillRequested)
            throw new InvalidOperationException(
                "Payment can only begin after the bill is requested."
            );

        Status = TableSessionStatus.PaymentPending;

        MarkUpdated();
    }

    public void Close()
    {
        if (Status != TableSessionStatus.PaymentPending)
            throw new InvalidOperationException(
                "Session can only be closed after reaching payment pending."
            );

        Status = TableSessionStatus.Closed;
        ClosedAt = DateTime.UtcNow;

        MarkUpdated();
    }
}
