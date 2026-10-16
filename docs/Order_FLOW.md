# Order relationships and the send-order request (MVP)

How an order and its lines fit together, and the exact steps of WBS 4.1 "send order". Modifiers are v2 (decision D-03); a free-text note per line replaces them.

## 1. Core relationship

```text
TableSession (visit)
  1
  │ has many
  ▼
  *
Order            = one kitchen ticket
  1
  │ owns (Order.Items)
  ▼
  *
OrderItem        = one product on the ticket, with its price snapshot
```

**One visit → many tickets → each ticket has many lines.**

## 2. Real example

```text
Table 4 · visit opened 12:30 by waiter Abebe
│
└── Order (Submitted 12:41)
      ├── Burger  × 2   250.00   note: "no onion"    = 500.00
      ├── Coke    × 3    40.00                       = 120.00
      └── Coffee  × 1    35.00                       =  35.00
                                                    --------
                                         Order total  655.00
```

The bill for the visit is the sum of all its orders' totals.

## 3. Fields and keys

```text
┌──────────────────┐
│      Order       │
│──────────────────│
│ ID               │
│ TableSessionId   │──► TableSession.ID
│ WaiterId         │──► User.ID
│ Status           │
│ SubmittedAt …    │
│ ServedAt         │  (MVP)
└────────┬─────────┘
         │ 1 : many   (cascade delete)
         ▼
┌──────────────────────┐          ┌──────────────────┐
│      OrderItem       │          │     MenuItem     │
│──────────────────────│  many:1  │──────────────────│
│ ID                   │─────────►│ ID               │
│ OrderId              │          │ Name             │
│ MenuItemId           │          │ Price            │
│ ItemName      (MVP)  │◄─copied──│                  │
│ UnitPrice            │◄─copied──│                  │
│ Quantity             │          └──────────────────┘
│ SpecialInstructions  │
└──────────────────────┘
```

| Key | Points to | Answers |
| --- | --- | --- |
| `OrderItem.OrderId` | `Order.ID` | Which ticket is this line on? |
| `OrderItem.MenuItemId` | `MenuItem.ID` | Which product was it? (Reporting only; the price comes from the snapshot) |
| `Order.TableSessionId` | `TableSession.ID` | Which visit pays for it? |
| `Order.WaiterId` | `User.ID` | Who sent it? |

## 4. The send-order request (WBS 4.1)

```json
POST /api/sessions/{visitId}/orders
{
  "lines": [
    { "menuItemId": "3f6c…", "quantity": 2, "note": "no onion" },
    { "menuItemId": "9a21…", "quantity": 3 }
  ]
}
```

There is no price in the request. The server decides every price.

`OrderService.SendOrderAsync` does this, in order:

1. Load the visit: 404 if missing, 403 if `visit.WaiterId != me.Id`, 409 if it isn't Open.
2. Load all requested menu items in one query; 409 if any is missing, inactive or unavailable.
3. `var order = new Order(visit.ID, me.Id);` (match the real constructor), then per line `order.AddItem(item.ID, item.Name, item.Price, line.Quantity, line.Note)`.
4. `order.Submit()`, which throws if there are no lines.
5. `db.Orders.Add(order); await db.SaveChangesAsync(ct);` That's one save, so the order and all its lines are stored together or not at all.
6. Return the order with its lines and `Total`.

A Draft order exists only in memory during this request (decision D-04).

## 5. When modifiers come back (v2)

```text
OrderItem ──1:*──► OrderItemModifier   (Name + AdditionalPrice copied from Modifier)

LineTotal = (UnitPrice + Σ AdditionalPrice) × Quantity
```

The tables already exist. Decide first whether a modifier is global (as in the code today) or belongs to one menu item (as the older docs drew it).
