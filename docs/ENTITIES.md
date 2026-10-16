# Entity reference (MVP)

Every entity: its fields and methods in the code today, what the MVP adds, and what waits for v2. The rules behind these fields are in [DOMAIN_MODEL.md](DOMAIN_MODEL.md).

All entities inherit `Entity`: `ID` (a `Guid`, spelled with a capital D), `CreatedAt`, `UpdatedAt` and `MarkUpdated()`.

## People

### User: employee account

- **Code today:** `FirstName`, `LastName`, `FullName`, `Email` (lower-cased), `Role`, `IsActive`; `ChangeName`, `ChangeEmail`, `ChageRole`, `Activate`, `Deactivate`.
- **MVP change:** add `PasswordHash` (nullable) and `SetPasswordHash()` (1.2); rename `ChageRole` → `ChangeRole` (1.2); unique `Email` (1.3).
- **v2:** separate `Username` (v1 logs in with email), password reset, lockout, Identity tables.

## Floor

### RestaurantTable: physical table

- **Code today:** `Number`, `Capacity`, `Status`, `IsActive`; `ChangeNumber`, `ChangeCapacity` (only when Available), `Occupy`, `MakeAvailable`, `Activate`, `Deactivate`.
- **MVP change:** unique `Number` (1.3).
- **Note:** status is only Available or Occupied. Bill states live on `TableSession`.

### TableSession: one visit at one table

- **Code today:** `RestaurantTableId`, `WaiterId`, `Status`, `GuestCount`, `OpenedAt`, `BillRequestedAt`, `ClosedAt`; `ChangeGuestCount`, `RequestBill`, `MarkPaymentPending`, `Close`.
- **MVP change:** add `ReturnToBillRequested()` (1.2); foreign keys and the one-open-visit-per-table index (1.3).

## Menu

### MenuCategory: group of items (Breakfast, Burgers, Drinks…)

- **Code today:** `Name`, `Description`, `DisplayOrder`, `IsActive`; `ChangeName`, `ChangeDescription`, `ChangeDisplayOrder`, `Activate`, `Deactivate`. The constructor is `(name, displayOrder, description)`.
- **Known bug:** `ChangeName` validates the new name but never assigns it.
- **MVP change:** fix `ChangeName` and set `UpdatedAt` (1.2); throw `DomainException` instead of `ArgumentNullException` (1.2).
- **v2:** delete. In v1 you deactivate instead, so old orders keep their references.

### MenuItem: product for sale

- **Code today:** `MenuCategoryId`, `Name`, `Description`, `Price` (0 allowed), `IsAvailable`, `IsActive`; `UpdateDetails`, `ChangePrice`, `MakeAvailable` (blocked when inactive), `MakeUnavailable`, `Activate`, `Deactivate` (also makes it unavailable). The constructor is `(menuCategoryId, name, description, price)`.
- **MVP change:** foreign key to the category and `numeric(12,2)` price (1.3). Service rule: new items go only into active categories (3.1).
- **Remember:** `IsActive` means "is it on the menu at all?" `IsAvailable` means "can we sell it right now?"

### Modifier and OrderItemModifier: v2

Tables exist and stay untouched apart from money precision. No v1 code reads or writes them (decision D-03).

## Ordering

### Order: one kitchen ticket

- **Code today:** `TableSessionId`, `WaiterId`, `Status`, `SubmittedAt`, `PreparingAt`, `ReadyAt`; `Submit`, `StartPreparing`, `MarkRead`.
- **MVP change (1.2):** `Items` (backed by a private `_items` list), `AddItem(menuItemId, itemName, unitPrice, quantity, note)` while Draft, `Total`, `Submit` requires at least one line, rename `MarkRead` → `MarkReady`, add `MarkServed` and `ServedAt`. Foreign keys (1.3).
- **v2:** `OrderNumber`, `CollectedAt`, cancel or void.

### OrderItem: one product on a ticket

- **Code today:** `OrderId`, `MenuItemId`, `Quantity`, `UnitPrice`, `SpecialInstructions`, `Totalprice`; `ChageQuantity`, `ChangeSpecialInstructions`.
- **Known bug:** the constructor checks the still-empty `OrderId` property instead of the `orderId` argument, so every construction throws. No `OrderItem` row can exist yet, which makes the new required column below safe.
- **MVP change (1.2):** fix the constructor; add `ItemName` (snapshot); rename `Totalprice` → `LineTotal` (= `UnitPrice` × `Quantity`).
- **Pricing in v1:** `LineTotal = UnitPrice × Quantity`. In v2, modifiers add to the unit price.

## Money

### Payment: the money event for a visit

- **Code today:** `TableSessionId`, `Amount`, `Method`, `Status`, `Reference`, `VerifiedByCashierId`, `VerifiedAt`; `Verify`, `Reject`, `MarkFailed`.
- **Known bug:** `Verify` and `MarkFailed` throw `IndexOutOfRangeException` for a business rule.
- **MVP change (1.2):** throw `DomainException`; the constructor requires `Reference` for Telebirr and CBE; `Reject(cashierId, reason)` stores `RejectionReason` and `RejectedAt`. Foreign key, money precision and the one-pending-payment index (1.3).
- **v2:** `RecordedByWaiterId` (v1 uses `TableSession.WaiterId`), `MarkFailed` callers.

### Receipt: proof of a verified payment

- **Code today:** `TableSessionId`, `PaymentId`, `ReceiptNumber`, `Amount`, `IssuedAt`.
- **MVP change (1.3):** unique `ReceiptNumber`, unique `PaymentId`, foreign keys, money precision. Number format `R-YYYYMMDD-####` (D-07).
- **v2:** `IssuedByCashierId`. In v1, trace it as `Receipt → Payment → VerifiedByCashierId`.

## Enums: make every value explicit (WBS 1.2)

The values below match today's implicit declaration order, so writing them out changes no stored data. It does stop a reordering from silently breaking the database filters.

| Enum | Values | v1 note |
| --- | --- | --- |
| `OrderStatus` | Draft = 0, Submitted = 1, Preparing = 2, Ready = 3, Collected = 4, Served = 5 | Collected unused |
| `TableSessionStatus` | Open = 0, BillRequested = 1, PaymentPending = 2, Closed = 3 | — |
| `TableStatus` | Available = 0, Occupied = 1 | Delete the commented-out members |
| `PaymentStatus` | Pending = 0, Verified = 1, Rejected = 2, Failed = 3 | Failed unused |
| `PaymentMethod` | Cash = 0, Telebirr = 1, CBE = 2 | — |
| `UserRole` | Waiter = 0, Kitchen = 1, Cashier = 2, Manager = 3 | The token's role claim uses the name, e.g. `Manager` |

## The WBS 1.3 migration (`MvpIntegrity`)

Put every v1 schema change into this one additive migration, so later phases need none. Never edit `InitialCreate`. Table names below assume EF's defaults from your `DbSet` property names; check `InitialCreate` for the real ones.

| Table | Change |
| --- | --- |
| Users | + `PasswordHash` (text, null); unique index on `Email` |
| RestaurantTables | Unique index on `Number` |
| TableSessions | FK → RestaurantTables, FK `WaiterId` → Users; partial unique index on `RestaurantTableId` where `Status <> 3` |
| MenuItems | FK → MenuCategories; `Price` numeric(12,2) |
| Orders | FK → TableSessions, FK `WaiterId` → Users; + `ServedAt` (null) |
| OrderItems | FK → Orders (cascade), FK → MenuItems; + `ItemName` (varchar 120, required); `UnitPrice` numeric(12,2) |
| Payments | FK → TableSessions, FK `VerifiedByCashierId` → Users; `Amount` numeric(12,2); + `RejectionReason` (varchar 200, null), `RejectedAt` (null); partial unique index on `TableSessionId` where `Status = 0` |
| Receipts | FK → Payments, FK → TableSessions; unique `ReceiptNumber`; unique `PaymentId`; `Amount` numeric(12,2) |
| Modifiers, OrderItemModifiers | Money columns numeric(12,2) only |

All foreign keys use `DeleteBehavior.Restrict`, except OrderItems → Orders, which cascades.

### One configuration as the pattern

```csharp
// Infrastructure/Persistence/Configurations/TableSessionConfiguration.cs
public sealed class TableSessionConfiguration : IEntityTypeConfiguration<TableSession>
{
    public void Configure(EntityTypeBuilder<TableSession> b)
    {
        b.HasOne<RestaurantTable>().WithMany()
         .HasForeignKey(s => s.RestaurantTableId).OnDelete(DeleteBehavior.Restrict);

        b.HasOne<User>().WithMany()
         .HasForeignKey(s => s.WaiterId).OnDelete(DeleteBehavior.Restrict);

        b.HasIndex(s => s.RestaurantTableId)
         .IsUnique()
         .HasFilter("\"Status\" <> 3")                 // 3 = Closed: one open visit per table
         .HasDatabaseName("UX_TableSessions_OneOpenPerTable");
    }
}
```

The other patterns you need:

```csharp
b.Property(p => p.Amount).HasPrecision(12, 2);                         // money

b.HasMany(o => o.Items).WithOne()                                       // Order owns its lines
 .HasForeignKey(i => i.OrderId).OnDelete(DeleteBehavior.Cascade);

// RestaurantPosDbContext
protected override void OnModelCreating(ModelBuilder modelBuilder) =>
    modelBuilder.ApplyConfigurationsFromAssembly(typeof(RestaurantPosDbContext).Assembly);
```

After generating the migration, read its `Up()` before applying it: you should see only added columns, indexes and foreign keys, and nothing dropped.
