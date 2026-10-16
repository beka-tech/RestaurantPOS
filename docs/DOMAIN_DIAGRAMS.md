# Domain diagrams

The domain model in pictures: the classes and how they refer to each other, and the state machine of every record that changes status. The rules in words are in [DOMAIN_MODEL.md](DOMAIN_MODEL.md); the tables are in [ERD.md](ERD.md).

The diagrams are Mermaid. PNG copies are in [diagrams/](diagrams/).

## 1. Class diagram

Entities refer to each other **by ID** (`WaiterId`, `TableSessionId`…), not by navigation properties. The one exception is `Order`, which owns its lines through `Items`. An arrow points from the class holding the ID to the class it names. **★ marks a member added or changed in WBS 1.2** (a new field, a renamed method or a bug fix).

<!-- diagram: domain-classes -->
```mermaid
classDiagram
    direction LR

    class User {
        <<root>>
        +Guid ID
        +string FirstName
        +string LastName
        +string Email
        +UserRole Role
        +bool IsActive
        +string ★PasswordHash
        +★ChangeRole(role)
        +★SetPasswordHash(hash)
        +Activate()
        +Deactivate()
    }

    class RestaurantTable {
        <<root>>
        +Guid ID
        +int Number
        +int Capacity
        +TableStatus Status
        +bool IsActive
        +Occupy()
        +MakeAvailable()
    }

    class TableSession {
        <<root · the visit>>
        +Guid ID
        +Guid RestaurantTableId
        +Guid WaiterId
        +TableSessionStatus Status
        +int GuestCount
        +DateTime OpenedAt
        +RequestBill()
        +MarkPaymentPending()
        +★ReturnToBillRequested()
        +Close()
    }

    class MenuCategory {
        <<root>>
        +Guid ID
        +string Name
        +int DisplayOrder
        +bool IsActive
        +★ChangeName(name)
        +Activate()
        +Deactivate()
    }

    class MenuItem {
        <<root>>
        +Guid ID
        +Guid MenuCategoryId
        +string Name
        +decimal Price
        +bool IsAvailable
        +bool IsActive
        +ChangePrice(price)
        +MakeAvailable()
        +MakeUnavailable()
    }

    class Order {
        <<root>>
        +Guid ID
        +Guid TableSessionId
        +Guid WaiterId
        +OrderStatus Status
        +IReadOnlyCollection~OrderItem~ ★Items
        +decimal ★Total
        +★AddItem(menuItemId, name, price, qty, note)
        +Submit()
        +StartPreparing()
        +★MarkReady()
        +★MarkServed()
    }

    class OrderItem {
        <<owned by Order>>
        +Guid ID
        +Guid OrderId
        +Guid MenuItemId
        +string ★ItemName
        +decimal UnitPrice
        +int Quantity
        +string SpecialInstructions
        +decimal ★LineTotal
    }

    class Payment {
        <<root>>
        +Guid ID
        +Guid TableSessionId
        +decimal Amount
        +PaymentMethod Method
        +PaymentStatus Status
        +string Reference
        +Guid VerifiedByCashierId
        +string ★RejectionReason
        +Verify(cashierId)
        +★Reject(cashierId, reason)
    }

    class Receipt {
        <<root>>
        +Guid ID
        +Guid TableSessionId
        +Guid PaymentId
        +string ReceiptNumber
        +decimal Amount
        +DateTime IssuedAt
    }

    TableSession "0..*" --> "1" RestaurantTable : RestaurantTableId
    TableSession "0..*" --> "1" User : WaiterId
    Order "0..*" --> "1" TableSession : TableSessionId
    Order "0..*" --> "1" User : WaiterId
    Order "1" *-- "1..*" OrderItem : Items
    OrderItem "0..*" --> "1" MenuItem : MenuItemId
    MenuItem "0..*" --> "1" MenuCategory : MenuCategoryId
    Payment "0..*" --> "1" TableSession : TableSessionId
    Payment "0..*" --> "0..1" User : VerifiedByCashierId
    Receipt "0..1" --> "1" Payment : PaymentId
    Receipt "0..1" --> "1" TableSession : TableSessionId
```

**Roots and ownership:** each `<<root>>` is loaded and saved on its own. `OrderItem` is created only through `Order.AddItem()` and saved with its order. Enum values are listed in [ENTITIES.md](ENTITIES.md#enums-make-every-value-explicit-wbs-12).

## 2. State machines

Every arrow is a domain method. Any transition that isn't drawn throws `DomainException`, which the API returns as 409.

### Order

<!-- diagram: state-order -->
```mermaid
stateDiagram-v2
    direction LR
    [*] --> Draft : new Order() + AddItem()
    Draft --> Submitted : Submit() by waiter
    Submitted --> Preparing : StartPreparing() by kitchen
    Preparing --> Ready : MarkReady() by kitchen
    Ready --> Served : MarkServed() by waiter
    Served --> [*]
    note right of Draft
        Lives only in memory during
        the send-order request.
        Never saved as Draft.
    end note
```

`Collected` stays in the enum but is skipped in v1.

### Visit (TableSession)

<!-- diagram: state-visit -->
```mermaid
stateDiagram-v2
    direction LR
    [*] --> Open : waiter opens the table
    Open --> BillRequested : RequestBill()
    BillRequested --> PaymentPending : MarkPaymentPending() when payment recorded
    PaymentPending --> BillRequested : ReturnToBillRequested() when payment rejected
    PaymentPending --> Closed : Close() inside Verify
    Closed --> [*]
```

### Table (RestaurantTable)

<!-- diagram: state-table -->
```mermaid
stateDiagram-v2
    direction LR
    [*] --> Available
    Available --> Occupied : Occupy() in the same save that opens a visit
    Occupied --> Available : MakeAvailable() in the same save that closes it
```

### Payment

<!-- diagram: state-payment -->
```mermaid
stateDiagram-v2
    direction LR
    [*] --> Pending : waiter records it
    Pending --> Verified : Verify(cashierId)
    Pending --> Rejected : Reject(cashierId, reason)
    Verified --> [*] : receipt issued, visit closed, table freed
    Rejected --> [*] : waiter records a new payment
```

`Failed` stays in the enum but is unused in v1.

## 3. How the states move together

One sale, step by step. Each row is one request and one save; a dash means the record doesn't exist yet.

| Step (who) | Table | Visit | Order | Payment | Receipt |
| --- | --- | --- | --- | --- | --- |
| Open visit (waiter) | **Occupied** | **Open** | — | — | — |
| Send order (waiter) | Occupied | Open | **Submitted** | — | — |
| Start (kitchen) | Occupied | Open | **Preparing** | — | — |
| Ready (kitchen) | Occupied | Open | **Ready** | — | — |
| Serve (waiter) | Occupied | Open | **Served** | — | — |
| Request bill (waiter) | Occupied | **BillRequested** | Served | — | — |
| Record payment (waiter) | Occupied | **PaymentPending** | Served | **Pending** | — |
| *Reject (cashier)* | Occupied | **BillRequested** | Served | **Rejected** | — |
| *Record again (waiter)* | Occupied | **PaymentPending** | Served | **Pending** (new) | — |
| Verify (cashier) | **Available** | **Closed** | Served | **Verified** | **Issued** |

Bold marks what that step changed. The last row is the all-or-nothing save described in the [payment doc](RestaurantPOS-Payment-Receipt-Mental-Model.md#4-verify-the-all-or-nothing-step-wbs-52).
