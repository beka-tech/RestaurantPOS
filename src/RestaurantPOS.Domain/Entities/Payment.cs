using RestaurantPOS.Domain.Common;
using RestaurantPOS.Domain.Enums;

namespace RestaurantPOS.Domain.Entities;

public class Payment : Entity
{
    public Guid TableSessionId { get; private set; }
    public decimal Amount { get; private set; }
    public PaymentMethod Method { get; private set; }
    public PaymentStatus Status { get; private set; }
    public string? Reference { get; private set; }
    public Guid? VerifiedByCashierId { get; private set; }
    public DateTime? VerifiedAt { get; private set; }

    public Payment(
        Guid tableSessionId,
        decimal amount,
        PaymentMethod method,
        string? reference = null
    )
    {
        if (tableSessionId == Guid.Empty)
            throw new ArgumentException("Table session is required. ");

        if (amount <= 0)
            throw new ArgumentException("payment amount must be greater than zero. ");

        TableSessionId = tableSessionId;
        Amount = amount;
        Method = method;

        Reference = string.IsNullOrWhiteSpace(reference) ? null : reference.Trim();

        Status = PaymentStatus.Pending;
    }

    public void Verify(Guid cashierId)
    {
        if (cashierId == Guid.Empty)
            throw new ArgumentException("Cashier is required. ");

        if (Status != PaymentStatus.Pending)
            throw new IndexOutOfRangeException("Only a Pending payment can be verified. ");

        Status = PaymentStatus.Verified;
        VerifiedByCashierId = cashierId;
        VerifiedAt = DateTime.UtcNow;

        MarkUpdated();
    }

    public void Reject(Guid cashierId)
    {
        if (cashierId == Guid.Empty)
            throw new ArgumentException("Cashier is required. ");

        if (Status != PaymentStatus.Pending)
            throw new InvalidOperationException("Only a Pending payment can be rejected. ");

        Status = PaymentStatus.Rejected;
        VerifiedByCashierId = cashierId;

        MarkUpdated();
    }

    public void MarkFailed()
    {
        if (Status != PaymentStatus.Pending)
            throw new IndexOutOfRangeException("Only a Pending Payment can fail. ");

        Status = PaymentStatus.Failed;

        MarkUpdated();
    }
}
