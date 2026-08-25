# RestaurantPOS — Payment & Receipt

## 🧠 Big Mental Model

Think of `Payment` and `Receipt` as the final part of a restaurant visit.

```text
Customer finishes eating
        │
        ▼
   Bill Requested
        │
        ▼
     Payment
        │
        │ cashier verifies
        ▼
 Payment Verified
        │
        ▼
     Receipt
        │
        ▼
TableSession Closed
        │
        ▼
RestaurantTable Available
```

The shortest memory model is:

```text
Payment
= MONEY EVENT

Receipt
= PROOF OF THE MONEY EVENT
```

---

# 1. 💳 Payment

`Payment` represents the customer's payment for a table session.

Example:

```text
Table 4
│
└── TableSession #500
      │
      └── Payment
            Amount: 850 ETB
            Method: Telebirr
            Status: Pending
```

## Payment contains

```text
Payment
│
├── Id
├── TableSessionId
├── Amount
├── Method
├── Status
├── Reference
├── VerifiedByCashierId
└── VerifiedAt
```

Think of the fields as questions:

```text
TableSessionId
    ↓
Which restaurant visit is being paid?

Amount
    ↓
How much is being paid?

Method
    ↓
How is the customer paying?

Status
    ↓
What is happening with the payment?

Reference
    ↓
Is there a transaction/reference number?

VerifiedByCashierId
    ↓
Which cashier confirmed the payment?

VerifiedAt
    ↓
When was it confirmed?
```

---

# 2. 💵 Payment Methods

For the current RestaurantPOS MVP:

```text
                  Payment
                     │
         ┌───────────┼───────────┐
         ▼           ▼           ▼
       Cash       Telebirr       CBE
```

Mental model:

```text
Cash
= Customer gives physical money

Telebirr
= Customer transfers digitally

CBE
= Customer transfers through CBE
```

The payment entity keeps the method:

```text
Payment.Method
      │
      ├── Cash
      ├── Telebirr
      └── CBE
```

---

# 3. 🔄 Payment Status

A payment has a lifecycle.

```text
                 Payment Created
                       │
                       ▼
                    Pending
                       │
                 Cashier checks
                       │
            ┌──────────┼──────────┐
            ▼          ▼          ▼
         Verified   Rejected    Failed
            │
            ▼
         Receipt
```

## Status mental model

```text
Pending
= Payment exists, but has not been confirmed yet.

Verified
= Cashier confirmed that payment was received.

Rejected
= Cashier checked it and rejected it.

Failed
= Payment process failed.
```

Important:

```text
Pending
   │
   ▼
Verified
   │
   ▼
Receipt
```

A receipt should be created only after successful payment verification.

---

# 4. 👤 Cashier Verification

The cashier is responsible for confirming the payment.

```text
Customer
   │
   │ pays
   ▼
Payment
   │
   │ checked by
   ▼
Cashier
   │
   │ Verify(...)
   ▼
Payment Verified
```

Example:

```text
Payment
│
├── Amount = 850 ETB
├── Method = Telebirr
├── Status = Verified
├── VerifiedByCashierId = CASHIER-12
└── VerifiedAt = 2026-08-25 ...
```

The key field is:

```text
VerifiedByCashierId
        │
        ▼
      User
   role = Cashier
```

So the system can answer:

> Who verified this payment?

---

# 5. 🔗 Payment → TableSession Relationship

A payment belongs to a `TableSession`.

```text
TableSession
      1
      │
      │ has payment
      ▼
   Payment
```

Foreign key:

```text
Payment.TableSessionId
          │
          ▼
TableSession.Id
```

Example:

```text
┌──────────────────┐
│   TableSession   │
│──────────────────│
│ Id = 500         │
└────────▲─────────┘
         │
         │ TableSessionId = 500
         │
┌────────┴─────────┐
│     Payment      │
│──────────────────│
│ Id = 700         │
│ TableSessionId=500
│ Amount = 850     │
└──────────────────┘
```

This means:

```text
Payment.TableSessionId
= "Which restaurant visit does this money belong to?"
```

---

# 6. 🧾 Receipt

A `Receipt` is the record created after payment has been verified.

Think:

```text
Payment
= We received the money.

Receipt
= Here is the proof that we received it.
```

Example:

```text
Payment
Amount: 850 ETB
Status: Verified
      │
      ▼
Receipt
ReceiptNumber: RCP-2026-00125
Amount: 850 ETB
IssuedAt: ...
```

## Receipt contains

```text
Receipt
│
├── Id
├── TableSessionId
├── PaymentId
├── ReceiptNumber
├── Amount
└── IssuedAt
```

Think of the fields as questions:

```text
PaymentId
    ↓
Which payment created this receipt?

TableSessionId
    ↓
Which restaurant visit does it belong to?

ReceiptNumber
    ↓
What unique receipt number identifies it?

Amount
    ↓
How much was paid?

IssuedAt
    ↓
When was the receipt created?
```

---

# 7. 🔗 Payment → Receipt Relationship

This is the core relationship:

```text
Payment
   1
   │
   │ produces
   ▼
Receipt
```

Foreign key:

```text
Receipt.PaymentId
        │
        ▼
Payment.Id
```

Example:

```text
┌──────────────────┐
│     Payment      │
│──────────────────│
│ Id = 700         │
│ Amount = 850     │
│ Status=Verified  │
└────────▲─────────┘
         │
         │ PaymentId = 700
         │
┌────────┴─────────┐
│     Receipt      │
│──────────────────│
│ Id = 900         │
│ PaymentId = 700  │
│ Amount = 850     │
└──────────────────┘
```

This means:

```text
Receipt.PaymentId
= "Which payment produced me?"
```

---

# 8. 🔗 Receipt → TableSession Relationship

The receipt can also point directly back to the table session.

```text
Receipt.TableSessionId
          │
          ▼
TableSession.Id
```

So you can move through the system like this:

```text
Receipt
   │
   ├── PaymentId
   │      │
   │      ▼
   │   Payment
   │
   └── TableSessionId
          │
          ▼
      TableSession
```

---

# 9. 🧩 Full Relationship Diagram

```text
┌──────────────────────┐
│     TableSession     │
│──────────────────────│
│ Id                   │
│ RestaurantTableId    │
│ WaiterId             │
│ Status               │
└──────────┬───────────┘
           │
           │ TableSessionId
           ▼
┌──────────────────────┐
│       Payment        │
│──────────────────────│
│ Id                   │
│ TableSessionId       │
│ Amount               │
│ Method               │
│ Status               │
│ Reference            │
│ VerifiedByCashierId  │──────────────► User / Cashier
│ VerifiedAt           │
└──────────┬───────────┘
           │
           │ PaymentId
           ▼
┌──────────────────────┐
│       Receipt        │
│──────────────────────│
│ Id                   │
│ TableSessionId       │
│ PaymentId            │
│ ReceiptNumber        │
│ Amount               │
│ IssuedAt             │
└──────────────────────┘
```

---

# 10. 🧠 Read the Relationships Like Sentences

```text
TableSession
    │
    └── has a Payment

Payment
    │
    ├── belongs to a TableSession
    ├── uses a PaymentMethod
    ├── has a PaymentStatus
    ├── can be verified by a Cashier
    └── creates a Receipt after verification

Receipt
    │
    ├── belongs to a Payment
    └── belongs to a TableSession
```

---

# 11. 🔑 Foreign-Key Memory Map

```text
Payment.TableSessionId
        │
        └──────────────► TableSession.Id


Payment.VerifiedByCashierId
        │
        └──────────────► User.Id
                          (Cashier)


Receipt.PaymentId
        │
        └──────────────► Payment.Id


Receipt.TableSessionId
        │
        └──────────────► TableSession.Id
```

Remember:

```text
TableSessionId
= Which restaurant visit?

PaymentId
= Which payment?

VerifiedByCashierId
= Which cashier?
```

---

# 12. 🍽️ Real Restaurant Story

Customer at Table 4 asks for the bill.

```text
Table 4
   │
   ▼
TableSession #500
   │
   ▼
Bill Requested
```

The bill is:

```text
Total = 850 ETB
```

Customer chooses Telebirr:

```text
Payment
│
├── TableSessionId = 500
├── Amount = 850 ETB
├── Method = Telebirr
└── Status = Pending
```

Then the cashier checks the merchant app:

```text
Cashier
   │
   ▼
Payment.Verify(cashierId)
```

Now:

```text
Payment
│
├── Amount = 850 ETB
├── Method = Telebirr
├── Status = Verified
├── VerifiedByCashierId = CASHIER-12
└── VerifiedAt = ...
```

After verification:

```text
Verified Payment
      │
      ▼
   Receipt
```

Receipt:

```text
Receipt
│
├── PaymentId = 700
├── TableSessionId = 500
├── ReceiptNumber = RCP-2026-00125
├── Amount = 850 ETB
└── IssuedAt = ...
```

Then:

```text
Receipt Created
      │
      ▼
TableSession Closed
      │
      ▼
Table Available
```

---

# 13. ⭐ Payment vs Receipt

This distinction is the most important part.

```text
              PAYMENT
                 │
                 │
      "Did we receive money?"
                 │
                 ▼
              VERIFIED
                 │
                 ▼
              RECEIPT
                 │
                 │
       "Proof that we received it"
```

Or simply:

| Entity | Meaning |
|---|---|
| `Payment` | The money transaction |
| `Receipt` | Proof/record of the successful transaction |

---

# 14. 🧠 Final Mental Model

Keep this picture in your head:

```text
              TableSession
                   │
                   │ owes bill
                   ▼
                Payment
                   │
                   │ cashier verifies
                   ▼
             Payment Verified
                   │
                   │ generates
                   ▼
                Receipt
                   │
                   │ closes
                   ▼
              TableSession
```

## Quick Memory

```text
BILL
 ↓
PAYMENT
 ↓
VERIFY
 ↓
RECEIPT
 ↓
CLOSE TABLE
```

### One-line rule

> **Payment records the money. Receipt proves the verified payment happened.**
