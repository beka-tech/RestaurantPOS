# RestaurantPOS — Order Relationships

## Core Relationship

```text
Order
  1
  │
  │ has many
  ▼
  *
OrderItem
  1
  │
  │ has many
  ▼
  *
OrderItemModifier
```

So:

```text
1 Order
   └── MANY OrderItems

1 OrderItem
   └── MANY OrderItemModifiers
```

---

## Real Example

```text
Order #101
│
├── OrderItem: Burger
│      │
│      ├── OrderItemModifier: Extra Cheese
│      └── OrderItemModifier: No Onion
│
├── OrderItem: Pizza
│      │
│      └── OrderItemModifier: Extra Mushroom
│
└── OrderItem: Coke
```

---

## Foreign-Key Relationship

### `OrderItem` points to `Order`

```csharp
public Guid OrderId { get; private set; }
```

```text
Order
Id = 100
   ▲
   │
   │ OrderId = 100
   │
OrderItem
Id = 200
```

`OrderItem.OrderId` answers:

> Which Order do I belong to?

---

### `OrderItemModifier` points to `OrderItem`

```csharp
public Guid OrderItemId { get; private set; }
```

```text
OrderItem
Id = 200
   ▲
   │
   │ OrderItemId = 200
   │
OrderItemModifier
Id = 300
Name = "Extra Cheese"
```

`OrderItemModifier.OrderItemId` answers:

> Which ordered food does this customization belong to?

---

## Bigger Domain Relationship

```text
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
```

More accurately:

```text
MenuItem
   1
   │
   │ can appear in many
   ▼
   *
OrderItem


Modifier
   1
   │
   │ can appear in many
   ▼
   *
OrderItemModifier
```

---

## Complete Relationship Diagram

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
         │ 1 : many
         │
         ▼
┌─────────────────┐          ┌─────────────────┐
│    OrderItem    │─────────►│    MenuItem     │
│─────────────────│  many:1  │─────────────────│
│ Id              │          │ Id              │
│ OrderId         │          │ Name            │
│ MenuItemId      │          │ Price           │
│ Quantity        │          └─────────────────┘
│ UnitPrice       │
└────────┬────────┘
         │
         │ 1 : many
         │
         ▼
┌──────────────────────┐       ┌─────────────────┐
│ OrderItemModifier    │──────►│    Modifier     │
│──────────────────────│ many:1│─────────────────│
│ Id                   │       │ Id              │
│ OrderItemId          │       │ Name            │
│ ModifierId           │       │ Price           │
│ Name                 │       └─────────────────┘
│ AdditionalPrice      │
└──────────────────────┘
```

---

## Read the Diagram Like a Sentence

```text
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
```

---

## ID Connection Map

```text
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
```

---

## Quick Memory

```text
ORDER
  │ 1
  │
  │ *
  ▼
ORDER ITEM
  │ 1
  │
  │ *
  ▼
ORDER ITEM MODIFIER
```

**One ticket → many foods → each food can have many customizations.**
