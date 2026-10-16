# Workflow map: one page

The whole MVP sale on one page: who acts, which endpoint, and what changes. Rules and reasons are in [DOMAIN_MODEL.md](DOMAIN_MODEL.md). All endpoints sit under `/api`.

## 1. The sale, step by step

| # | Who | Action | Endpoint | What changes |
| --- | --- | --- | --- | --- |
| 0 | Manager | Sets up menu, tables and staff | `categories`, `items`, `tables`, `employees` | Catalog and accounts exist |
| 1 | Waiter | Opens a table for N guests | `POST tables/{id}/sessions` | Table → Occupied; visit → Open |
| 2 | Waiter | Sends an order | `POST sessions/{id}/orders` | Order → Submitted, lines priced from the menu |
| 3 | Kitchen | Starts cooking | `POST orders/{id}/start` | Order → Preparing |
| 4 | Kitchen | Marks it ready | `POST orders/{id}/ready` | Order → Ready |
| 5 | Waiter | Serves it | `POST orders/{id}/served` | Order → Served |
| — | | *Repeat steps 2–5 for more orders* | | |
| 6 | Waiter | Requests the bill | `POST sessions/{id}/request-bill` | Visit → BillRequested |
| 7 | Waiter | Records the payment (Cash, Telebirr, CBE) | `POST sessions/{id}/payments` | Payment → Pending; visit → PaymentPending |
| 8a | Cashier | Verifies it | `POST payments/{id}/verify` | Payment → Verified, receipt issued, visit → Closed, table → Available (all at once) |
| 8b | Cashier | Rejects it, with a reason | `POST payments/{id}/reject` | Payment → Rejected; visit → BillRequested; back to step 7 |
| 9 | Cashier | Prints the receipt | `GET receipts/{id}` | Nothing |

**Memory:** `Waiter opens → Waiter sends → Kitchen cooks → Waiter serves → Waiter bills → Cashier verifies → Table free`

---

## 2. Order flow

```text
          WAITER
            │ Send order (Draft → Submit() in one request)
            ▼
        Submitted
            │
         KITCHEN
            │ StartPreparing()
            ▼
        Preparing
            │ MarkReady()
            ▼
          Ready
            │
          WAITER
            │ MarkServed()
            ▼
          Served
```

## 3. Visit (TableSession) flow

```text
Open
 │ RequestBill()
 ▼
BillRequested ◄──────────────┐
 │ MarkPaymentPending()      │ ReturnToBillRequested()
 ▼                           │ (payment rejected)
PaymentPending ──────────────┘
 │ Close()  (only inside Verify)
 ▼
Closed
```

## 4. Table flow

```text
Available ──Occupy()──► Occupied ──MakeAvailable()──► Available
           (opening the visit)      (closing the visit)
```

## 5. Payment flow

```text
Waiter records
     │
     ▼
  Pending
     │
  ┌──┴──────────┐
  ▼             ▼
Verify()      Reject(reason)
  │             │
  ▼             ▼
Verified      Rejected ──► waiter records a new payment
  │
  ▼
Receipt + visit Closed + table Available
```

---

## 6. Who does what

```text
WAITER   (only on visits they opened)
  ├── Open visit
  ├── Send order (lines + notes)
  ├── Mark order Served
  ├── Request bill
  └── Record payment

KITCHEN
  ├── See queue: Submitted, Preparing, Ready (oldest first)
  ├── StartPreparing()
  └── MarkReady()

CASHIER
  ├── See pending payments
  ├── Verify()  → receipt is issued automatically
  ├── Reject(reason)
  └── Print receipt

MANAGER
  ├── Menu: categories, items, price, availability
  ├── Tables
  ├── Employees: create, change role, deactivate
  └── Read receipts
```

A Manager has only these permissions. There is no automatic "Manager can do everything" in v1.
