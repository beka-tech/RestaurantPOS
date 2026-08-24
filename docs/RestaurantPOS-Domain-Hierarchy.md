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

Bigger Domain Relationship

                     MENU SIDE
                         │
                         ▼
                    MenuItem
                         │
                         │ customer chooses
                         ▼

Order ────────────► OrderItem
│
│ customized using
▼
Modifier
│
▼
OrderItemModifier

More accurately:

MenuItem
1
│
│ can appear in many
▼

- OrderItem

Modifier
1
│
│ can appear in many
▼

- OrderItemModifier

Complete Relationship Diagram

┌─────────────────┐
│ Order │
│─────────────────│
│ Id │
│ TableSessionId │
│ WaiterId │
│ Status │
└────────┬────────┘
│
│ 1 : many
│
▼
┌─────────────────┐ ┌─────────────────┐
│ OrderItem │─────────►│ MenuItem │
│─────────────────│ many:1 │─────────────────│
│ Id │ │ Id │
│ OrderId │ │ Name │
│ MenuItemId │ │ Price │
│ Quantity │ └─────────────────┘
│ UnitPrice │
└────────┬────────┘
│
│ 1 : many
│
▼
┌──────────────────────┐ ┌─────────────────┐
│ OrderItemModifier │──────►│ Modifier │
│──────────────────────│ many:1│─────────────────│
│ Id │ │ Id │
│ OrderItemId │ │ Name │
│ ModifierId │ │ Price │
│ Name │ └─────────────────┘
│ AdditionalPrice │
└──────────────────────┘

Read the Diagram Like a Sentence

Order
│
└── contains OrderItems

OrderItem
│
├── belongs to one Order
│
├── refers to one MenuItem
│
└── contains OrderItemModifiers

OrderItemModifier
│
├── belongs to one OrderItem
│
└── refers to one Modifier

ID Connection Map

OrderId
↓
connects OrderItem → Order

MenuItemId
↓
connects OrderItem → MenuItem

OrderItemId
↓
connects OrderItemModifier → OrderItem

ModifierId
↓
connects OrderItemModifier → Modifier

Quick Memory

ORDER
│ 1
│
│ _
▼
ORDER ITEM
│ 1
│
│ _
▼
ORDER ITEM MODIFIER

One ticket → many foods → each food can have many customizations
