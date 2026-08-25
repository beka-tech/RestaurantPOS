using RestaurantPOS.Domain.Common;

namespace RestaurantPOS.Domain.Entities;

public class Receipt : Entity
{
    public Guid TableSessionId { get; private set; }
    public Guid PaymentId { get; private set; }

    public string ReceiptNumber { get; private set; }
    public decimal Amount { get; private set; }
    public DateTime IssuedAt { get; private set; }

    public Receipt(Guid tableSessionId, Guid paymentId, string receiptNumber, decimal amount)
    {
        if (tableSessionId == Guid.Empty)
            throw new ArgumentException("Table session is required. ");

        if (paymentId == Guid.Empty)
            throw new ArgumentException("Payment is required. ");

        if (string.IsNullOrWhiteSpace(receiptNumber))
            throw new ArgumentException("Receipt number is required. ");

        if (amount <= 0)
            throw new ArgumentException("Receipt amount must be greater than zero. ");

        TableSessionId = tableSessionId;
        PaymentId = paymentId;
        ReceiptNumber = receiptNumber.Trim();
        Amount = amount;

        IssuedAt = DateTime.UtcNow;
    }
}
