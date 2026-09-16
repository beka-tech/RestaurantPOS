# RestaurantPOS Entity Reference

## 1. User

**Meaning:** Employee account.

```text
User
├── Waiter
├── Kitchen
├── Cashier
└── Manager
```

Key data:

```text
Name
Username
PasswordHash
Role
IsActive
```

Key behavior:

```text
ChangeName()
ChangeRole()
ChangePasswordHash()
Activate()
Deactivate()
```

---

## 2. RestaurantTable

**Meaning:** Physical table in the restaurant.

Key data:

```text
Number
Capacity
Status
IsActive
```

State:

```text
Available
   ↓
Occupied
   ↓
BillRequested
   ↓
PaymentPending
   ↓
Available
```

---

## 3. TableSession

**Meaning:** One complete customer visit at one table.

Key data:

```text
RestaurantTableId
WaiterId
Status
GuestCount
OpenedAt
BillRequestedAt
ClosedAt
```

State:

```text
Open
 ↓
BillRequested
 ↓
PaymentPending
 ↓
Closed
```

---

## 4. MenuCategory

**Meaning:** Group of menu items.

Examples:

```text
Breakfast
Burgers
Pizza
Drinks
Desserts
```

Key data:

```text
Name
DisplayOrder
IsActive
```

---

## 5. MenuItem

**Meaning:** Product sold by the restaurant.

Examples:

```text
Burger
Pizza
Coffee
Coke
Pasta
```

Key data:

```text
MenuCategoryId
Name
Description
Price
IsAvailable
IsActive
```

Remember:

```text
IsActive    = should it exist on the menu?
IsAvailable = can it be sold right now?
```

---

## 6. Modifier

**Meaning:** Available customization for a menu item.

```text
Burger
├── Extra Cheese +30
├── Extra Sauce  +15
└── No Onion      +0
```

Key data:

```text
MenuItemId
Name
PriceAdjustment
IsActive
```

---

## 7. Order

**Meaning:** One order sent through the kitchen workflow.

Key data:

```text
TableSessionId
OrderNumber
Status
Items
Total
ConfirmedAt
PreparingAt
ReadyAt
CollectedAt
ServedAt
```

State:

```text
Draft
 ↓
Confirmed
 ↓
Preparing
 ↓
Ready
 ↓
Collected
 ↓
Served
```

Kitchen mainly owns:

```text
Confirmed
   ↓
Preparing
   ↓
Ready
```

---

## 8. OrderItem

**Meaning:** One product actually ordered.

Example:

```text
Order
├── Burger ×2
├── Coke ×3
└── Coffee ×1
```

Key data:

```text
MenuItemId
ItemName
UnitPrice
Quantity
Notes
Modifiers
```

Pricing:

```text
UnitTotal = UnitPrice + ModifierTotal

Total = UnitTotal × Quantity
```

---

## 9. OrderItemModifier

**Meaning:** Modifier actually selected by the customer.

```text
Modifier
= what CAN be selected

OrderItemModifier
= what WAS selected
```

Key data:

```text
ModifierId
Name
PriceAdjustment
```

---

## 10. Payment

**Meaning:** Customer payment for a TableSession.

Key data:

```text
TableSessionId
RecordedByWaiterId
Amount
Method
Status
ReferenceNumber
VerifiedByCashierId
VerifiedAt
```

Flow:

```text
Pending
  │
  ├── Verify() ──► Verified
  │
  └── Reject() ──► Rejected
```

---

## 11. Receipt

**Meaning:** Final transaction record after successful payment verification.

Key data:

```text
TableSessionId
PaymentId
IssuedByCashierId
ReceiptNumber
TotalAmount
IssuedAt
```

Flow:

```text
Payment
   ↓
Verified
   ↓
Receipt
```

---

# One-Line Memory Map

```text
User
 │
 ├── Waiter ──► TableSession ──► Order ──► OrderItem ──► OrderItemModifier
 │                                  ▲
 │                                  │
 ├── Kitchen ───────────────────────┘
 │
 └── Cashier ──► Payment ──► Receipt


MenuCategory ──► MenuItem ──► Modifier
```
