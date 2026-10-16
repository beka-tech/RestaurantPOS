# Payment and receipt (MVP)

The last part of a visit, and the riskiest part of the build: real money, one receipt, and a table that must free up exactly once. This covers WBS phase 5.

## 1. Big mental model

```text
Customer finishes eating
        │
        ▼
  Bill requested            (waiter)
        │
        ▼
 Payment recorded           (waiter: Cash / Telebirr / CBE)  → Pending
        │
        │ cashier checks the cash drawer or the merchant app
        ├──────────────────────────────┐
        ▼                              ▼
 Payment Verified                Payment Rejected
        │                              │
        ▼                              └──► back to "Bill requested";
     Receipt                                waiter records a new payment
        │
        ▼
 Visit Closed + Table Available
```

```text
Payment = MONEY EVENT
Receipt = PROOF OF THE MONEY EVENT
```

---

## 2. The bill

- **Bill total** = Σ (`UnitPrice` × `Quantity`) over every line of every order in the visit.
- It uses only the **snapshot** prices on the lines, so a menu price change after sending doesn't touch it.
- Prices include tax; there is no service charge, discount or tip in v1 (decision D-02).
- Money is `decimal` in C# and `numeric(12,2)` in PostgreSQL. The currency is ETB.
- The bill can be requested only while the visit is **Open** (`TableSession.RequestBill()`).

`GET /api/sessions/{id}/bill` returns the lines and the total. The browser never sends a total.

---

## 3. Payment

### Fields

```text
Payment
├── ID
├── TableSessionId        → which visit is being paid?
├── Amount                → server-computed bill total
├── Method                → Cash | Telebirr | CBE
├── Status                → Pending | Verified | Rejected   (Failed unused in v1)
├── Reference             → transaction number from Telebirr or CBE
├── VerifiedByCashierId   → which cashier confirmed it?
├── VerifiedAt
├── RejectionReason       (MVP)
└── RejectedAt            (MVP)
```

### Reference rule (decision D-06)

| Method | Reference | Why |
| --- | --- | --- |
| Cash | Optional | The cashier counts the money |
| Telebirr | **Required** | The cashier matches it in the merchant app |
| CBE | **Required** | The cashier matches it in the bank app |

The `Payment` constructor enforces this, so no code path can skip it.

### Record a payment (WBS 5.2): `POST /api/sessions/{id}/payments`

```json
{ "method": "Telebirr", "reference": "TB-88123456" }
```

There is no amount in the request. `PaymentService.RecordAsync`:

1. Load the visit: 404 if missing, 403 if it isn't the caller's visit, 409 unless it is BillRequested.
2. Compute the bill total on the server.
3. `new Payment(visit.ID, total, method, reference)`. This throws `DomainException` (→ 409) when the reference is missing for Telebirr or CBE.
4. `visit.MarkPaymentPending()`.
5. Save once. The partial unique index allows only **one Pending payment per visit**, so a double-click gets a 409.

---

## 4. Verify: the all-or-nothing step (WBS 5.2)

`POST /api/payments/{id}/verify`, Cashier only.

```csharp
public async Task<ReceiptDto> VerifyAsync(Guid paymentId, CancellationToken ct)
{
    var payment = await db.Payments.FirstOrDefaultAsync(p => p.ID == paymentId, ct)
        ?? throw new NotFoundException("Payment", paymentId);
    var visit = await db.TableSessions.FirstAsync(s => s.ID == payment.TableSessionId, ct);
    var table = await db.RestaurantTables.FirstAsync(t => t.ID == visit.RestaurantTableId, ct);

    payment.Verify(me.Id);                 // Pending → Verified, else DomainException (409)
    var receipt = new Receipt(visit.ID, payment.ID, await NextReceiptNumberAsync(ct), payment.Amount);
    visit.Close();                         // PaymentPending → Closed
    table.MakeAvailable();                 // Occupied → Available

    db.Receipts.Add(receipt);
    await db.SaveChangesAsync(ct);         // ONE save = ONE transaction: all four or none
    return ReceiptDto.From(receipt);
}
```

Match the `Receipt` constructor's real parameter order. The cashier ID comes from the token (`me.Id`), never from the request.

**Why one `SaveChangesAsync` is enough:** EF Core wraps a single save in a database transaction. If the receipt insert fails, the payment, visit and table changes roll back with it.

**Double-click on Verify:** two requests can both load the payment while it is still Pending. Both build a receipt, and the unique index on `Receipts.PaymentId` rejects the second insert. The second request gets a 409 and nothing of it is saved.

---

## 5. Reject (WBS 5.2)

`POST /api/payments/{id}/reject` with `{ "reason": "Reference not found in Telebirr app" }`, Cashier only.

1. `payment.Reject(me.Id, reason)`: Pending → Rejected, storing the reason and time.
2. `visit.ReturnToBillRequested()`: PaymentPending → BillRequested.
3. Save once. The waiter can now record a new payment.

---

## 6. Receipt

```text
Receipt
├── ID
├── TableSessionId   → which visit?
├── PaymentId        → which payment produced me?  (unique)
├── ReceiptNumber    → R-20261011-0001             (unique)
├── Amount           → copied from the payment
└── IssuedAt
```

The cashier is not stored on the receipt. Trace it: `Receipt → Payment → VerifiedByCashierId → User`.

### Receipt number: `R-YYYYMMDD-####` (decision D-07)

```csharp
private async Task<string> NextReceiptNumberAsync(CancellationToken ct)
{
    var local = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, "Africa/Addis_Ababa");
    var prefix = $"R-{local:yyyyMMdd}-";
    var count = await db.Receipts.CountAsync(r => r.ReceiptNumber.StartsWith(prefix), ct);
    return $"{prefix}{count + 1:D4}";
}
```

The date uses restaurant time, not UTC, so the counter resets at local midnight. If two cashiers verify at the same instant, the unique index rejects the duplicate number, and the second cashier gets a 409 and clicks Verify again. That's fine for v1.

### Printing (WBS 5.3)

`/receipts/:id` is a plain Angular page with a print button that calls `window.print()`. An `@media print` stylesheet hides the navigation. There is no printer driver in v1.

---

## 7. Protection summary

| Mistake | What stops it |
| --- | --- |
| Paying a visit twice at once | Partial unique index: one Pending payment per visit |
| Two receipts for one payment | Unique `Receipts.PaymentId` |
| Duplicate receipt numbers | Unique `Receipts.ReceiptNumber` |
| Visit closed without a verified payment | `Close()` is called only inside Verify |
| Wrong amount from the browser | The amount is computed on the server; the request has no amount field |
| Telebirr or CBE without a reference | `Payment` constructor |
| Waiter verifying their own payment | `[Authorize(Roles = "Cashier")]` on verify and reject |

---

## 8. Quick memory

```text
BILL ─► PAYMENT (Pending) ─► VERIFY ─► RECEIPT ─► CLOSE VISIT ─► FREE TABLE
                     │
                     └─► REJECT ─► back to BILL
```

> **Payment records the money. Receipt proves the verified payment happened.**
