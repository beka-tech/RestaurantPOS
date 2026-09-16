# RestaurantPOS — Domain Hierarchy

## 🧠 Domain Mental Model

Think of the RestaurantPOS domain as a restaurant moving through one main flow:

```text
                           RESTAURANT POS
                                 │
        ┌────────────────────────┼────────────────────────┐
        │                        │                        │
        ▼                        ▼                        ▼
      USERS                    MENU                 RESTAURANT FLOOR
        │                        │                        │
        │                 MenuCategory                   │
        │                        │                  RestaurantTable
        │                        ▼                        │
        │                    MenuItem                    ▼
        │                        │                  TableSession
        │                        ▼                        │
        │                    Modifier                    │
        │                                                 │
        └──────────────────────────────┐                  │
                                       │                  │
                                       ▼                  ▼
                                    ORDERING
                                       │
                                       ▼
                                     Order
                                       │
                           ┌───────────┴───────────┐
                           │                       │
                           ▼                       ▼
                       OrderItem               OrderItem
                       "Burger"                 "Pizza"
                           │                       │
                    ┌──────┴──────┐                │
                    ▼             ▼                ▼
            OrderItemModifier  OrderItemModifier  OrderItemModifier
             "Extra Cheese"      "No Onion"       "Extra Mushroom"
                           │
                           │
                           ▼
                        KITCHEN
                           │
                           ▼
                    Order Status
                           │
                 ┌─────────┼─────────┐
                 ▼         ▼         ▼
              Submitted Preparing   Ready
                                     │
                                     ▼
                                  SERVED
                                     │
                                     ▼
                               BILL REQUESTED
                                     │
                                     ▼
                                  Payment
                                     │
                       ┌─────────────┼─────────────┐
                       ▼             ▼             ▼
                     Cash         Telebirr        CBE
                       │             │             │
                       └─────────────┴─────────────┘
                                     │
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
                           RestaurantTable
                               Available
```

## 🔍 Ordering Hierarchy

```text
RestaurantTable
      │
      ▼
TableSession
      │
      ▼
    Order
      │
      │ contains
      ▼
  OrderItem
      │
      │ customized by
      ▼
OrderItemModifier
```

## 🍔 Example

```text
Table 4
│
└── TableSession
      │
      └── Order #101
            │
            ├── OrderItem
            │     Burger × 1
            │       │
            │       ├── OrderItemModifier
            │       │     Extra Cheese +30 ETB
            │       │
            │       └── OrderItemModifier
            │             No Onion +0 ETB
            │
            ├── OrderItem
            │     Pizza × 1
            │       │
            │       └── OrderItemModifier
            │             Extra Mushroom +40 ETB
            │
            └── OrderItem
                  Coke × 2
```

## 🧠 Quick Memory

```text
RestaurantTable
      ↓
TableSession
      ↓
Order               = WHOLE TICKET
      ↓
OrderItem           = FOOD / DRINK
      ↓
OrderItemModifier   = CUSTOMIZATION
      ↓
Kitchen
      ↓
Payment
      ↓
Receipt
```

> **Mental shortcut:**  
> `Table → Session → Ticket → Food → Customization → Kitchen → Payment → Receipt`

## ⭐ Most Important Mini-Diagram

```text
Order
│
├── OrderItem
│     ├── OrderItemModifier
│     └── OrderItemModifier
│
├── OrderItem
│     └── OrderItemModifier
│
└── OrderItem
```

**Relationship:**

- One `Order` has many `OrderItem`s.
- One `OrderItem` can have many `OrderItemModifier`s.
- `Order` = whole ticket.
- `OrderItem` = food or drink ordered.
- `OrderItemModifier` = customization of that specific item.

---

# 🔗 Entity Relationships — Order, OrderItem, OrderItemModifier

## Big Relationship Picture

```text
                     ┌─────────────────┐
                     │      Order      │
                     │─────────────────│
                     │ Id              │
                     │ TableSessionId  │
                     │ WaiterId        │
                     │ Status          │
                     └────────┬────────┘
                              │
                              │ 1 : MANY
                              │
                              ▼
                     ┌─────────────────┐
                     │    OrderItem    │
                     │─────────────────│
                     │ Id              │
                     │ OrderId         │◄──────────────┐
                     │ MenuItemId      │               │
                     │ Quantity        │               │
                     │ UnitPrice       │               │
                     └────────┬────────┘               │
                              │                        │
                              │ 1 : MANY               │
                              │                        │
                              ▼                        │
                ┌────────────────────────┐             │
                │ OrderItemModifier      │             │
                │────────────────────────│             │
                │ Id                     │             │
                │ OrderItemId            │─────────────┘
                │ ModifierId             │
                │ Name                   │
                │ AdditionalPrice        │
                └────────────────────────┘
```

## Relationship in One Sentence

```text
1 Order
   │
   └── has MANY OrderItems
              │
              └── each OrderItem can have MANY OrderItemModifiers
```

Or:

```text
ORDER
  │
  │ 1 : many
  ▼
ORDER ITEM
  │
  │ 1 : many
  ▼
ORDER ITEM MODIFIER
```

---

## 🍔 Real Restaurant Example

```text
Order #101
│
├── OrderItem: Burger × 1
│      │
│      ├── OrderItemModifier: Extra Cheese +30 ETB
│      │
│      └── OrderItemModifier: No Onion +0 ETB
│
├── OrderItem: Pizza × 1
│      │
│      └── OrderItemModifier: Extra Mushroom +40 ETB
│
└── OrderItem: Coke × 2
```

Think:

```text
Order
= the whole ticket

OrderItem
= one food or drink on the ticket

OrderItemModifier
= customization attached to that food/drink
```

---

## 🔑 How the IDs Connect the Entities

### `OrderItem.OrderId` → `Order.Id`

```text
┌──────────────┐
│    Order     │
│──────────────│
│ Id = 100     │
└──────▲───────┘
       │
       │ OrderId = 100
       │
┌──────┴───────┐
│  OrderItem   │
│──────────────│
│ Id = 200     │
│ OrderId=100  │
└──────────────┘
```

`OrderId` answers:

> **Which Order does this OrderItem belong to?**

---

### `OrderItemModifier.OrderItemId` → `OrderItem.Id`

```text
┌─────────────────┐
│    OrderItem    │
│─────────────────│
│ Id = 200        │
│ Burger          │
└────────▲────────┘
         │
         │ OrderItemId = 200
         │
┌────────┴─────────────┐
│ OrderItemModifier    │
│──────────────────────│
│ Id = 300             │
│ OrderItemId = 200    │
│ Extra Cheese         │
└──────────────────────┘
```

`OrderItemId` answers:

> **Which ordered food does this customization belong to?**

---

# 🍽️ Menu Side vs Order Side

This distinction is important:

```text
MENU SIDE                         ORDER SIDE

MenuItem                          Order
"Burger"                            │
   │                                │
   │ customer chooses               ▼
   └──────────────────────────► OrderItem
                                "Burger × 2"
                                     │
                                     │ customized by
                                     ▼
                              OrderItemModifier
                               "Extra Cheese"
```

## `MenuItem` vs `OrderItem`

```text
MenuItem
"What CAN be ordered?"

        │
        │ customer chooses it
        ▼

OrderItem
"What WAS actually ordered?"
```

Example:

```text
MenuItem
Burger
Price: 250 ETB
       │
       ▼
OrderItem
Burger × 2
UnitPrice: 250 ETB
SpecialInstructions: "No tomato"
```

---

# 🧩 Modifier Side

A reusable `Modifier` is the definition:

```text
Modifier
"Extra Cheese"
```

When the customer chooses it for a specific ordered food, it becomes:

```text
OrderItemModifier
"Extra Cheese"
AdditionalPrice = 30 ETB
```

Relationship:

```text
Modifier
   1
   │
   │ can appear in many orders
   ▼
   *
OrderItemModifier
```

And:

```text
MenuItem
   1
   │
   │ can appear in many orders
   ▼
   *
OrderItem
```

---

# 🧠 Full Ordering Relationship Map

```text
                        ┌───────────────┐
                        │     Order     │
                        └───────┬───────┘
                                │
                                │ 1 : MANY
                                ▼
                        ┌───────────────┐
                        │   OrderItem   │
                        └───┬───────┬───┘
                            │       │
               MANY : 1     │       │ 1 : MANY
                            │       │
                            ▼       ▼
                      ┌──────────┐  ┌───────────────────┐
                      │ MenuItem │  │ OrderItemModifier │
                      └──────────┘  └─────────┬─────────┘
                                              │
                                              │ MANY : 1
                                              ▼
                                         ┌──────────┐
                                         │ Modifier │
                                         └──────────┘
```

Read it like this:

```text
Order
  │
  └── contains many OrderItems

OrderItem
  │
  ├── belongs to one Order
  │
  ├── refers to one MenuItem
  │
  └── can contain many OrderItemModifiers

OrderItemModifier
  │
  ├── belongs to one OrderItem
  │
  └── refers to one Modifier
```

---

# 🧠 Foreign-Key Memory Map

```text
OrderItem.OrderId
        │
        └──────────────► Order.Id


OrderItem.MenuItemId
        │
        └──────────────► MenuItem.Id


OrderItemModifier.OrderItemId
        │
        └──────────────► OrderItem.Id


OrderItemModifier.ModifierId
        │
        └──────────────► Modifier.Id
```

Think of each foreign key as an **arrow pointing to the entity it belongs to**.

---

# ⭐ Final Mental Model

```text
                   ORDER
              "Whole Ticket"
                    │
                    │ contains
                    ▼
               ORDER ITEM
             "Food / Drink"
                    │
                    │ customized by
                    ▼
          ORDER ITEM MODIFIER
              "Extra / Change"
```

### Memory Shortcut

```text
Ticket
  ↓
Food
  ↓
Customization
```

Or:

```text
Order
   ↓
"What whole ticket?"

OrderItem
   ↓
"What food/drink?"

OrderItemModifier
   ↓
"How is that food customized?"
```

**One ticket → many foods → each food can have many customizations.**

---

# 💳 Payment & Receipt Relationships

## Big Mental Picture

After the customer finishes eating, the table session moves into payment.

```text
RestaurantTable
      │
      ▼
TableSession
      │
      ▼
Bill Requested
      │
      ▼
   Payment
      │
      │ verified by cashier
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

The simplest memory model is:

```text
TableSession
     ↓
Payment
     ↓
Receipt
```

Think:

```text
TableSession
= What visit/table bill are we paying for?

Payment
= How was that bill paid?

Receipt
= Proof that the payment was completed
```

---

## 💳 `Payment` — The Money Transaction

A `Payment` represents the customer's payment for the table session bill.

Example:

```text
Table 4
│
└── TableSession #500
      │
      └── Payment
            Amount: 850 ETB
            Method: Telebirr
            Status: Verified
```

Your `Payment` contains information like:

```text
Payment
├── Id
├── TableSessionId
├── Amount
├── Method
├── Status
├── Reference
├── VerifiedByCashierId
└── VerifiedAt
```

Mental model:

```text
PAYMENT
│
├── Which table bill?
│      └── TableSessionId
│
├── How much?
│      └── Amount
│
├── How did they pay?
│      └── Cash / Telebirr / CBE
│
├── Current payment state?
│      └── Pending / Verified / Rejected / Failed
│
└── Who confirmed it?
       └── VerifiedByCashierId
```

---

## 🧾 `Receipt` — Proof of a Verified Payment

A `Receipt` is created after a payment has been successfully verified.

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

Your `Receipt` contains information like:

```text
Receipt
├── Id
├── TableSessionId
├── PaymentId
├── ReceiptNumber
├── Amount
└── IssuedAt
```

Mental model:

```text
RECEIPT
│
├── Which table session?
│      └── TableSessionId
│
├── Which payment?
│      └── PaymentId
│
├── Receipt identity?
│      └── ReceiptNumber
│
├── How much was paid?
│      └── Amount
│
└── When was it issued?
       └── IssuedAt
```

---

# 🔗 Core Relationships

For the current RestaurantPOS MVP:

```text
TableSession
     1
     │
     │ has payment
     ▼
   Payment
     1
     │
     │ produces
     ▼
   Receipt
```

Or:

```text
TABLE SESSION
      │
      │ "This table owes money"
      ▼
   PAYMENT
      │
      │ "The money was verified"
      ▼
   RECEIPT
      │
      │ "Proof of payment"
      ▼
 SESSION CLOSED
```

---

# 🔑 Foreign-Key Relationships

## `Payment.TableSessionId` → `TableSession.Id`

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
│ TableSessionId   │
│ Amount           │
│ Method           │
│ Status           │
└──────────────────┘
```

`Payment.TableSessionId` answers:

> **Which table session is this payment paying for?**

---

## `Receipt.PaymentId` → `Payment.Id`

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

`Receipt.PaymentId` answers:

> **Which verified payment created this receipt?**

---

## `Receipt.TableSessionId` → `TableSession.Id`

The receipt also stores the table session relationship directly:

```text
TableSession
Id = 500
   ▲
   │
   │ TableSessionId = 500
   │
Receipt
Id = 900
```

This makes it easy to answer:

> **Which restaurant visit / table session does this receipt belong to?**

---

# 👤 Cashier Relationship

A cashier verifies the payment.

```text
              Cashier / User
                    │
                    │ verifies
                    ▼
                 Payment
                    │
                    └── VerifiedByCashierId
```

Important distinction:

```text
Cashier
   ↓
verifies Payment

Receipt
   ↓
records proof of that verified Payment
```

The cashier does not need to be the direct parent of `Receipt` because the relationship can already be traced:

```text
Receipt
   ↓
Payment
   ↓
VerifiedByCashierId
   ↓
Cashier / User
```

---

# 💵 Payment Method & Status Flow

```text
                    Payment
                       │
          ┌────────────┼────────────┐
          ▼            ▼            ▼
        Cash        Telebirr        CBE
          │            │            │
          └────────────┼────────────┘
                       ▼
                    Pending
                       │
                Cashier checks
                       │
              ┌────────┴────────┐
              ▼                 ▼
           Verified          Rejected
              │
              ▼
           Receipt
```

Status mental model:

```text
Payment
   │
   ├── Pending
   │
   ├── Verified ─────► Receipt
   │
   ├── Rejected
   │
   └── Failed
```

A receipt should only be created for a **verified payment**.

---

# 🍽️ Full Restaurant Flow with Payment

```text
RestaurantTable
      │
      ▼
TableSession
      │
      ▼
    Order
      │
      ▼
  OrderItem
      │
      ▼
OrderItemModifier
      │
      ▼
   Kitchen
      │
      ▼
    Ready
      │
      ▼
    Served
      │
      ▼
Bill Requested
      │
      ▼
   Payment
      │
      ├── Cash
      ├── Telebirr
      └── CBE
      │
      ▼
Cashier Verification
      │
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

---

# 🧠 Entity Relationship Map

```text
┌────────────────────┐
│   TableSession     │
│────────────────────│
│ Id                 │
│ RestaurantTableId  │
│ WaiterId           │
│ Status             │
└─────────┬──────────┘
          │
          │ payment belongs to session
          ▼
┌────────────────────┐
│      Payment       │
│────────────────────│
│ Id                 │
│ TableSessionId     │
│ Amount             │
│ Method             │
│ Status             │
│ Reference          │
│ VerifiedByCashierId│
│ VerifiedAt         │
└─────────┬──────────┘
          │
          │ verified payment
          │ produces
          ▼
┌────────────────────┐
│      Receipt       │
│────────────────────│
│ Id                 │
│ TableSessionId     │
│ PaymentId          │
│ ReceiptNumber      │
│ Amount             │
│ IssuedAt           │
└────────────────────┘
```

---

# 🧠 ID Connection Map

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

Think of each ID as an arrow:

```text
TableSessionId
= "Which restaurant visit?"

PaymentId
= "Which payment?"

VerifiedByCashierId
= "Which cashier confirmed it?"
```

---

# ⭐ Payment vs Receipt

```text
PAYMENT
"What happened to the money?"

        │
        │ after successful verification
        ▼

RECEIPT
"What proof do we give/store?"
```

Example:

```text
Customer pays 850 ETB by Telebirr
              │
              ▼
           Payment
     Method = Telebirr
     Amount = 850
     Status = Pending
              │
              ▼
      Cashier checks payment
              │
              ▼
        Payment.Verify(...)
              │
              ▼
     Status = Verified
              │
              ▼
           Receipt
     ReceiptNumber = RCP-...
     Amount = 850
```

---

# ⭐ Final Mental Model

```text
TableSession
    │
    │ bill belongs to
    ▼
 Payment
    │
    │ when verified
    ▼
 Receipt
```

Remember:

```text
Payment
= MONEY EVENT

Receipt
= PROOF OF MONEY EVENT
```

And the complete shortcut:

```text
Table
  ↓
Session
  ↓
Order
  ↓
Food
  ↓
Kitchen
  ↓
Bill
  ↓
Payment
  ↓
Verification
  ↓
Receipt
  ↓
Close Session
```
