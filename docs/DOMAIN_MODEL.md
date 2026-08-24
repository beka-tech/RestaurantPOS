# RestaurantPOS Domain Model

## 1. Big Picture

The domain can be understood as three connected areas.

```text
                    RESTAURANTPOS
                         │
          ┌──────────────┼──────────────┐
          │              │              │
          ▼              ▼              ▼
       PEOPLE          SERVICE          MENU
          │              │              │
        User      RestaurantTable   MenuCategory
                         │              │
                         ▼              ▼
                    TableSession     MenuItem
                         │              │
                         ▼              ▼
                       Order         Modifier
                         │
                         ▼
                     OrderItem
                         │
                         ▼
                OrderItemModifier
                         │
                         ▼
                      Payment
                         │
                         ▼
                      Receipt
```

## 2. Actor Responsibilities

### Waiter

```text
Waiter
  │
  ├── opens TableSession
  ├── creates Order
  ├── adds OrderItems
  ├── confirms Order
  ├── collects ready Order
  ├── serves Order
  └── records Payment
```

### Kitchen

```text
Kitchen User
     │
     ▼
Confirmed Order
     │
     ├── StartPreparing()
     ▼
Preparing
     │
     ├── reads OrderItems
     ├── reads Notes
     ├── reads OrderItemModifiers
     │
     └── MarkReady()
     ▼
Ready
```

### Cashier

```text
Cashier
   │
   ▼
Pending Payment
   │
   ├── Verify()
   │      └── Verified
   │
   └── Reject()
          └── Rejected
```

### Manager

The Manager is currently modeled as a `User` role. Management features can be added later without requiring a separate `Manager` entity.

---

## 3. Main Aggregate Flow

```text
RestaurantTable
      │
      ▼
TableSession
      │
      ├──────────────► Payment
      │                  │
      │                  ▼
      │               Receipt
      │
      ▼
    Order
      │
      ▼
  OrderItem
      │
      ▼
OrderItemModifier
```

`TableSession` is the central record for one customer visit.

---

## 4. Menu Configuration Flow

```text
MenuCategory
      │
      ▼
   MenuItem
      │
      ▼
   Modifier
```

Example:

```text
Burgers
   │
   ▼
Beef Burger — 250 ETB
   │
   ├── Extra Cheese +30
   ├── Extra Sauce  +15
   └── No Onion      +0
```

---

## 5. Snapshot Pattern

The domain separates current menu configuration from historical order data.

```text
Current configuration                 Historical snapshot

MenuItem                              OrderItem
Name: Burger                          ItemName: Burger
Price: 300                            UnitPrice: 250

Modifier                              OrderItemModifier
Name: Extra Cheese                    Name: Extra Cheese
PriceAdjustment: 40                   PriceAdjustment: 30
```

This means an old transaction does not change when the restaurant changes today's menu.

---

## 6. Order Lifecycle

```text
                 Waiter
                   │
                   ▼
                 Draft
                   │
                Confirm()
                   ▼
               Confirmed
                   │
              Kitchen sees
                   │
           StartPreparing()
                   ▼
               Preparing
                   │
               MarkReady()
                   ▼
                 Ready
                   │
            Waiter collects
                   ▼
               Collected
                   │
             Waiter serves
                   ▼
                Served
```

Invalid jumps should be blocked.

```text
Draft ───────────────► Ready       ✖
Ready ───────────────► Served      ✖
Served ──────────────► Preparing   ✖
```

---

## 7. Table Lifecycle

```text
Available
   │
   │ customers seated
   ▼
Occupied
   │
   │ bill requested
   ▼
BillRequested
   │
   │ payment started
   ▼
PaymentPending
   │
   │ session completed
   ▼
Available
```

---

## 8. TableSession Lifecycle

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

---

## 9. Payment Lifecycle

```text
Customer pays
     │
     ▼
Waiter records payment
     │
     ▼
   Pending
     │
  ┌──┴─────────────┐
  │                │
  ▼                ▼
Verify()          Reject()
  │                │
  ▼                ▼
Verified         Rejected
```

Supported payment methods:

```text
Cash
Telebirr
CBE
```

Electronic payments require a reference number.

---

## 10. Future Kitchen Extension

Do not add this for the MVP unless needed.

A larger restaurant may later introduce:

```text
KitchenStation
      │
      ├── Grill
      ├── Pizza
      ├── Bar
      └── Dessert
```

Then menu items could be routed:

```text
Burger ─────► Grill
Pizza ──────► Pizza Station
Coffee ─────► Bar
Cake ───────► Dessert
```

For the current MVP, `UserRole.Kitchen` plus the `Order` workflow is sufficient.
