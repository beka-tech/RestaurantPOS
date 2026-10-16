# Entity-relationship diagrams (ERD)

The PostgreSQL schema the MVP runs on: the tables from `InitialCreate` plus the WBS 1.3 `MvpIntegrity` migration. Field-by-field notes are in [ENTITIES.md](ENTITIES.md); the rules behind the keys are in [DOMAIN_MODEL.md](DOMAIN_MODEL.md).

The diagrams are Mermaid, so GitHub and VS Code (with a Mermaid preview extension) draw them from the text below. PNG copies are in [diagrams/](diagrams/); regenerate them after you edit a diagram (see [How to regenerate the PNGs](#how-to-regenerate-the-pngs)).

## 1. At a glance

Nine tables carry the whole sale. The visit (`TableSessions`) is in the middle: orders hang off it on one side and payments on the other.

<!-- diagram: erd-overview -->
```mermaid
erDiagram
    RestaurantTables ||--o{ TableSessions : "hosts"
    Users ||--o{ TableSessions : "opens"
    TableSessions ||--o{ Orders : "has"
    Users ||--o{ Orders : "sends"
    Orders ||--|{ OrderItems : "contains"
    MenuCategories ||--o{ MenuItems : "groups"
    MenuItems ||--o{ OrderItems : "sold as"
    TableSessions ||--o{ Payments : "paid by"
    Users |o--o{ Payments : "verifies"
    Payments ||--o| Receipts : "proved by"
    TableSessions ||--o| Receipts : "closed with"
```

**How to read the crow's feet:** `||` means exactly one, `|o` zero or one, `|{` one or more, and `o{` zero or more. So "Orders `||--|{` OrderItems" means every line belongs to one order, and an order has at least one line.

## 2. Full MVP schema

Every table also has `CreatedAt` and `UpdatedAt` (timestamptz), which are left out of the diagram to save space. Money columns are `numeric(12,2)`. Enum columns store integers; the comment shows the values.

<!-- diagram: erd-mvp -->
```mermaid
erDiagram
    Users {
        uuid ID PK
        text FirstName
        text LastName
        text Email UK "stored lower-case"
        int Role "0 Waiter, 1 Kitchen, 2 Cashier, 3 Manager"
        bool IsActive
        text PasswordHash "MVP, nullable"
    }
    RestaurantTables {
        uuid ID PK
        int Number UK
        int Capacity
        int Status "0 Available, 1 Occupied"
        bool IsActive
    }
    TableSessions {
        uuid ID PK
        uuid RestaurantTableId FK "one non-Closed visit per table"
        uuid WaiterId FK
        int Status "0 Open, 1 BillRequested, 2 PaymentPending, 3 Closed"
        int GuestCount
        timestamptz OpenedAt
        timestamptz BillRequestedAt "nullable"
        timestamptz ClosedAt "nullable"
    }
    MenuCategories {
        uuid ID PK
        text Name
        text Description "nullable"
        int DisplayOrder
        bool IsActive
    }
    MenuItems {
        uuid ID PK
        uuid MenuCategoryId FK
        text Name
        text Description "nullable"
        numeric Price "12,2"
        bool IsAvailable
        bool IsActive
    }
    Orders {
        uuid ID PK
        uuid TableSessionId FK
        uuid WaiterId FK
        int Status "1 Submitted, 2 Preparing, 3 Ready, 5 Served"
        timestamptz SubmittedAt
        timestamptz PreparingAt "nullable"
        timestamptz ReadyAt "nullable"
        timestamptz ServedAt "MVP, nullable"
    }
    OrderItems {
        uuid ID PK
        uuid OrderId FK "cascade delete"
        uuid MenuItemId FK
        varchar ItemName "MVP, name snapshot"
        numeric UnitPrice "price snapshot, 12,2"
        int Quantity "at least 1"
        text SpecialInstructions "note, nullable"
    }
    Payments {
        uuid ID PK
        uuid TableSessionId FK "one Pending payment per visit"
        numeric Amount "server-computed bill, 12,2"
        int Method "0 Cash, 1 Telebirr, 2 CBE"
        int Status "0 Pending, 1 Verified, 2 Rejected"
        text Reference "required for Telebirr and CBE"
        uuid VerifiedByCashierId FK "nullable"
        timestamptz VerifiedAt "nullable"
        varchar RejectionReason "MVP, nullable"
        timestamptz RejectedAt "MVP, nullable"
    }
    Receipts {
        uuid ID PK
        uuid TableSessionId FK
        uuid PaymentId FK, UK "one receipt per payment"
        text ReceiptNumber UK "R-YYYYMMDD-####"
        numeric Amount "12,2"
        timestamptz IssuedAt
    }

    RestaurantTables ||--o{ TableSessions : "hosts"
    Users ||--o{ TableSessions : "opens (WaiterId)"
    TableSessions ||--o{ Orders : "has"
    Users ||--o{ Orders : "sends (WaiterId)"
    Orders ||--|{ OrderItems : "contains"
    MenuCategories ||--o{ MenuItems : "groups"
    MenuItems ||--o{ OrderItems : "sold as"
    TableSessions ||--o{ Payments : "paid by"
    Users |o--o{ Payments : "verifies (VerifiedByCashierId)"
    Payments ||--o| Receipts : "proved by"
    TableSessions ||--o| Receipts : "closed with"
```

## 3. Keys that protect money and data

These are the constraints that do real work. The partial indexes are what stop double-clicks and races; see [DOMAIN_MODEL.md §6](DOMAIN_MODEL.md#6-invariants-and-who-enforces-them).

| Table | Constraint | Stops |
| --- | --- | --- |
| TableSessions | Unique `RestaurantTableId` where `Status <> 3` | Two open visits on one table |
| Payments | Unique `TableSessionId` where `Status = 0` | Two pending payments for one visit |
| Receipts | Unique `PaymentId` | Two receipts for one payment (double-click on Verify) |
| Receipts | Unique `ReceiptNumber` | Duplicate receipt numbers |
| RestaurantTables | Unique `Number` | Two "Table 3"s |
| Users | Unique `Email` | Two accounts with one login |
| All foreign keys | `Restrict` on delete (OrderItems → Orders cascades) | Deleting a menu item, table or user that history points to |

Table names assume EF's defaults from your `DbSet` property names. Check `InitialCreate` for the real names, and rename them here if they differ.

## 4. v2 extension: modifiers

These two tables already exist but stay unused in v1 (decision D-03). When modifiers come back, a line can carry customisations, each with its own name and price snapshot.

<!-- diagram: erd-v2-modifiers -->
```mermaid
erDiagram
    OrderItems {
        uuid ID PK
        varchar ItemName
        numeric UnitPrice
        int Quantity
    }
    Modifiers {
        uuid ID PK
        text Name "e.g. Extra cheese"
        numeric AdditionalPrice "12,2"
        bool IsActive
    }
    OrderItemModifiers {
        uuid ID PK
        uuid OrderItemId FK
        uuid ModifierId FK
        text Name "snapshot"
        numeric AdditionalPrice "snapshot, 12,2"
    }
    OrderItems ||--o{ OrderItemModifiers : "customised by"
    Modifiers ||--o{ OrderItemModifiers : "chosen as"
```

In v2: `LineTotal = (UnitPrice + Σ AdditionalPrice) × Quantity`.

## How to regenerate the PNGs

```powershell
npm i -g @mermaid-js/mermaid-cli
# copy one mermaid block into a file, then:
mmdc -i erd-mvp.mmd -o docs/diagrams/erd-mvp.png -b white -s 2
```
