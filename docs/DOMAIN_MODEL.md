# Domain model (MVP)

The restaurant's rules as the MVP enforces them: how each record moves through its states, and which layer stops each mistake. Field lists are in [ENTITIES.md](ENTITIES.md); the order of work is in [BUILD_PLAN.md](BUILD_PLAN.md). The same model as rendered diagrams: [DOMAIN_DIAGRAMS.md](DOMAIN_DIAGRAMS.md) (classes and state machines) and [ERD.md](ERD.md) (tables).

## 1. Big picture

```text
RestaurantTable ──1:*──► TableSession  (the visit)
                              │
                 ┌────────────┴────────────┐
                 ▼ 1:*                     ▼ 1:*
               Order                    Payment ──1:0..1──► Receipt
                 │ 1:*
                 ▼
             OrderItem ──*:1──► MenuItem ──*:1──► MenuCategory
          (name + price copied
           from MenuItem when sent)

User (one of 4 roles) appears as:
  TableSession.WaiterId · Order.WaiterId · Payment.VerifiedByCashierId

v2 only: Modifier, OrderItemModifier (tables exist, unused in v1)
```

`TableSession` is the centre of the model: one visit owns its orders and its payment.

**Memory:** `Table → Visit → Orders → Lines → Bill → Payment → Receipt → Visit closed → Table free`

---

## 2. Order lifecycle

```text
 (Draft) ──Submit()──► Submitted ──StartPreparing()──► Preparing ──MarkReady()──► Ready ──MarkServed()──► Served
  waiter, in the        kitchen sees it                 kitchen                    kitchen                waiter
  "send order" request
```

- **Draft lives only inside the send-order request.** The service creates the order, adds the lines, calls `Submit()` and saves once. A Draft order is never stored (decision D-04).
- `Collected` stays in the enum but is unused in v1: Ready goes straight to Served.
- Any jump not drawn above throws `DomainException`, which the API returns as 409.

| Code today | MVP change (WBS 1.2) |
| --- | --- |
| `Submit`, `StartPreparing`, `MarkRead` (typo) | Rename `MarkRead` → `MarkReady`; add `MarkServed` and `ServedAt` |
| No lines, no total | Add `Items`, `AddItem(...)` (Draft only) and `Total`; `Submit` requires at least one line |

---

## 3. Visit (TableSession) and table lifecycle

```text
TableSession:
  Open ──RequestBill()──► BillRequested ──MarkPaymentPending()──► PaymentPending ──Close()──► Closed
                                ▲                                        │
                                └────── ReturnToBillRequested() ─────────┘
                                         (cashier rejected the payment)

RestaurantTable:
  Available ──Occupy()──► Occupied ──MakeAvailable()──► Available
            same save as             same save as
            opening the visit        closing the visit
```

- `TableStatus` has only **Available** and **Occupied**. The bill states live on `TableSession`, not on the table (older docs drew them on the table).
- `Close()` is called only inside the cashier's Verify operation, never from its own endpoint. That's how "no visit closes unpaid" is guaranteed.
- MVP change (WBS 1.2): add `ReturnToBillRequested()` (PaymentPending → BillRequested), used when a payment is rejected.

---

## 4. Payment lifecycle

```text
 Waiter records ──► Pending ──Verify(cashierId)──────────► Verified ──► receipt issued, visit Closed, table Available
                       │
                       └──Reject(cashierId, reason)──► Rejected ──► visit back to BillRequested;
                                                                     waiter records a new payment
```

- `Failed` stays in the enum but is unused in v1 (there is no provider integration).
- Payment methods: **Cash**, **Telebirr**, **CBE**. Telebirr and CBE need a reference (D-06).
- MVP change (WBS 1.2): `DomainException` replaces `IndexOutOfRangeException`; the constructor checks the reference rule; `Reject` takes a reason and stores `RejectionReason` and `RejectedAt`.

Full algorithms: [payment and receipt](RestaurantPOS-Payment-Receipt-Mental-Model.md).

---

## 5. Snapshot rule

The menu changes; history must not.

```text
Current configuration                 Historical snapshot (copied when the order is sent)

MenuItem                              OrderItem
Name:  Burger                         ItemName:  Burger
Price: 300                            UnitPrice: 250
```

- The service copies `Name` and `Price` from `MenuItem` onto the line. It never takes a price from the browser.
- The bill, payment amount and receipt are computed only from line snapshots.
- MVP change (WBS 1.2): add `ItemName` to `OrderItem`.

---

## 6. Invariants and who enforces them

Rules that lose money or data when broken get a database guard as well as a code guard.

| # | Rule | Domain | Service | Database (WBS 1.3) |
| --- | --- | --- | --- | --- |
| 1 | A table has at most one visit that isn't Closed | `Occupy()` needs Available | — | Partial unique index on `TableSessions(RestaurantTableId)` where Status ≠ Closed |
| 2 | Only the visit's waiter can order, request the bill or record payment | — | `visit.WaiterId == me.Id`, else 403 | — |
| 3 | An order has at least one line; quantity ≥ 1 | `Submit()`, `OrderItem` constructor | DataAnnotations on the request | — |
| 4 | Prices come from the server | — | Look up `MenuItem`; ignore any client price | — |
| 5 | Only active and available items can be ordered | `IsActive`, `IsAvailable` flags | Check both before `AddItem` | — |
| 6 | Statuses move only along the drawn arrows | Entity methods | — | — |
| 7 | Payment amount = bill total when recorded | — | Server computes it | `numeric(12,2)` |
| 8 | Telebirr and CBE carry a reference | `Payment` constructor | — | — |
| 9 | One Pending payment per visit | — | — | Partial unique index on `Payments(TableSessionId)` where Status = Pending |
| 10 | One receipt per payment; receipt numbers unique | — | Verify creates exactly one | Unique `Receipts(PaymentId)` and `Receipts(ReceiptNumber)` |
| 11 | Verify is all or nothing: payment Verified + receipt + visit Closed + table Available | — | One `SaveChangesAsync` | One transaction |
| 12 | At least one active Manager remains | — | Count check before demote or deactivate | — |
| 13 | Inactive employees can't sign in | — | `AuthService.LoginAsync` | — |

Where two requests race (two waiters opening one table, a double-click on Verify), the code checks pass for both, and the database index stops the second. The API turns that into a 409.

---

## 7. Parked for v2

Don't build these until the demo script passes:

- Modifiers (`Modifier`, `OrderItemModifier`) and their pricing
- `Order.OrderNumber`, the `Collected` step, cancel or void
- Kitchen stations (Grill, Bar, Dessert routing)
- `Payment.RecordedByWaiterId`: use `TableSession.WaiterId` instead
- `Receipt.IssuedByCashierId`: trace it through `Payment.VerifiedByCashierId` instead
- `Failed` payments, provider callbacks, split bills, tips, tax lines
- Manager overrides, audit log table, SignalR push
