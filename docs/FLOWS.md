# Workflow Map — One Page

## 1. Order Flow

```text
          WAITER
            │
            ▼
          Draft
            │ Confirm()
            ▼
        Confirmed
            │
            ▼
         KITCHEN
            │ StartPreparing()
            ▼
        Preparing
            │ MarkReady()
            ▼
          Ready
            │
            ▼
          WAITER
            │ MarkCollected()
            ▼
        Collected
            │ MarkServed()
            ▼
          Served
```

### Memory

```text
Waiter sends → Kitchen cooks → Waiter serves
```

---

## 2. TableSession Flow

```text
Open
 │
 │ RequestBill()
 ▼
BillRequested
 │
 │ MarkPaymentPending()
 ▼
PaymentPending
 │
 │ Close()
 ▼
Closed
```

### Memory

```text
Visit starts → Bill → Payment → Close
```

---

## 3. RestaurantTable Flow

```text
Available
   │ Occupy()
   ▼
Occupied
   │ RequestBill()
   ▼
BillRequested
   │ MarkPaymentPending()
   ▼
PaymentPending
   │ MakeAvailable()
   ▼
Available
```

---

## 4. Payment Flow

```text
Customer pays
     │
     ▼
Waiter records
     │
     ▼
   Pending
     │
  ┌──┴─────────┐
  ▼            ▼
Verify()     Reject()
  │            │
  ▼            ▼
Verified     Rejected
  │
  ▼
Receipt
```

---

## 5. Who Does What?

```text
WAITER
  ├── Open TableSession
  ├── Create Order
  ├── Add OrderItems
  ├── Confirm Order
  ├── Collect Order
  ├── Serve Order
  └── Record Payment

KITCHEN
  ├── Read Order
  ├── Read OrderItems
  ├── Read Modifiers / Notes
  ├── StartPreparing()
  └── MarkReady()

CASHIER
  ├── Check Payment
  ├── Verify() / Reject()
  └── Issue Receipt
```
