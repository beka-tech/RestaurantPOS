# Restaurant POS docs

Start here. These docs describe the **lean MVP**: one complete dine-in sale, built by one developer in 15 working days.

**Source of truth:** code first, then these docs. If a doc and the code disagree, the code wins, and you fix the doc in the same commit.

## What to read, and when

| Doc | Read it when | What it answers |
| --- | --- | --- |
| [BUILD_PLAN.md](BUILD_PLAN.md) | Every morning | What to build today, in what order, and when it is done |
| [IMPLEMENTATION_GUIDE.md](IMPLEMENTATION_GUIDE.md) | Before coding a slice | How to write a service, controller, error mapping, JWT login and Angular wiring |
| [DOMAIN_MODEL.md](DOMAIN_MODEL.md) | Before touching a status or rule | Lifecycles, invariants, and where each rule is enforced |
| [ENTITIES.md](ENTITIES.md) | Before an EF configuration or migration | Fields per entity (code today vs MVP change), enums, the WBS 1.3 migration |
| [ERD.md](ERD.md) | Before an EF configuration or migration | Entity-relationship diagrams of the MVP schema, and the keys that protect money |
| [DOMAIN_DIAGRAMS.md](DOMAIN_DIAGRAMS.md) | Before writing a domain method | Class diagram, state machines, and how the states move together in one sale |
| [USE_CASES.md](USE_CASES.md) | Before building a screen | Use case diagram, 13 use case specs (flows and errors), and the full-sale sequence |
| [FLOWS.md](FLOWS.md) | When you forget who does what | One-page cheat sheet of the whole sale |
| [Order_FLOW.md](Order_FLOW.md) | Building WBS 4.1 | How an order and its lines are built and priced |
| [RestaurantPOS-Payment-Receipt-Mental-Model.md](RestaurantPOS-Payment-Receipt-Mental-Model.md) | Building phase 5 | Bill, payment, verify, reject, receipt and close |
| [APPLICATION_LAYER_GUIDE.md](APPLICATION_LAYER_GUIDE.md) | After v1 ships | **v2 track:** MediatR + CQRS workbook |
| [AUTHENTICATION_AUTHORIZATION_GUIDE.md](AUTHENTICATION_AUTHORIZATION_GUIDE.md) | After v1 ships | **v2 track:** Identity cookies, CSRF and ABAC workbook |

## MVP decisions (WBS 1.1)

These are the Day 1 decisions: seven product assumptions (D-01 to D-07) and two technical ones (D-08, D-09). Build on them; any change goes to v2. Link this section from the root `Readme.md`.

| ID | Decision | What it means in code |
| --- | --- | --- |
| D-01 | Dine-in only | Every order belongs to a `TableSession`; no takeaway flow |
| D-02 | Menu prices include tax; no service charge, discount or tip | Bill total = Σ (`UnitPrice` × `Quantity`) over the visit's lines |
| D-03 | No modifiers in v1 | `Modifier` and `OrderItemModifier` tables stay but are unused; each line has a free-text note |
| D-04 | An order can't be edited after it is sent | The order is created and submitted in one request; a Draft is never stored |
| D-05 | One full payment per visit | At most one Pending payment per visit; no split bills |
| D-06 | Telebirr and CBE payments need a reference; Cash doesn't | Enforced in the `Payment` constructor |
| D-07 | Receipt number `R-YYYYMMDD-####`, counter restarts daily | Unique index on `ReceiptNumber` |
| D-08 | The waiter who opened a visit owns it | Only that waiter can order, request the bill or record payment on it; no Manager override in v1 |
| D-09 | JWT bearer login, accounts created by a Manager | Deactivation takes effect at the next login (tokens last 8 h) |

## Words used in these docs

- **Visit**: a `TableSession`, meaning one group at one table from seating to payment.
- **Order**: one kitchen ticket. A visit can have several.
- **Line**: an `OrderItem`.
- **Snapshot**: the item name and price copied onto a line when the order is sent.
- **Code today**: exists in source now (per the 2026-10-11 roadmap review).
- **MVP change**: to do in the WBS row named next to it.
- **v2**: parked until the demo script passes.

## What changed in this docs pass (2026-10-11)

- Added this README, [IMPLEMENTATION_GUIDE.md](IMPLEMENTATION_GUIDE.md), and [BUILD_PLAN.md](BUILD_PLAN.md) (the plan export, renamed from `Solo Dev Build Plan #U2014 WBS & Implementation Guide.md`).
- Rewrote DOMAIN_MODEL, ENTITIES, FLOWS, Order_FLOW and the payment doc to use the names in the code (`Submit`/`Submitted`, not `Confirm`/`Confirmed`) and the MVP scope (no modifiers, no table bill states, no `Collected`).
- Removed `RestaurantPOS-Domain-Hierarchy.md`. Its diagrams are now in DOMAIN_MODEL, Order_FLOW and the payment doc; restore it from git if you want the original.
- Marked the two workbooks as the v2 track and kept their file paths, so the engineering-roadmap links still work.
- Added [ERD.md](ERD.md), [DOMAIN_DIAGRAMS.md](DOMAIN_DIAGRAMS.md) and [USE_CASES.md](USE_CASES.md), written as Mermaid so GitHub draws them from text. PNG copies are in `diagrams/`.
