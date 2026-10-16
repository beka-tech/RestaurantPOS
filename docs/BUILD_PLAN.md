# Restaurant POS — Lean MVP Build Plan (WBS)

2026-10-11 · Beka · Snapshot of the live plan doc. Track Status in one place only.

**Related docs:** [index and decisions](README.md) · [how to code each slice](IMPLEMENTATION_GUIDE.md) · [domain rules](DOMAIN_MODEL.md) · [entities and the 1.3 migration](ENTITIES.md)

## At a glance

Ship **one complete dine-in sale** in 15 working days: the Manager sets up menu, tables and staff; the Waiter opens a table and sends orders; the Kitchen prepares them; the Cashier verifies payment and prints a receipt. That is 84 planned hours plus a 6-hour buffer, assuming 6 focused hours a day. The 56-task engineering roadmap becomes your v2 backlog.

**Starting point (from the roadmap review):** Domain entities and an 11-table migration exist. There are no controllers, no auth and no Application code, and the Angular pages are placeholders. Two confirmed bugs block orders and renames: the `OrderItem` constructor and `MenuCategory.ChangeName`.

| Area | In the MVP | Out (v2 backlog) |
| --- | --- | --- |
| Staff and login | JWT login, 4 roles, Manager creates and deactivates employees | Cookie + CSRF, lockout, password reset, Identity tables |
| Menu | Categories and items: create, edit, price, available or not | Modifiers (use a free-text note per line), images |
| Tables | Manager creates tables; one open visit per table | Move or merge tables, reservations, takeaway |
| Orders | Waiter sends an order; Kitchen: Preparing → Ready; Waiter: Served | Draft editing, cancel or void, SignalR push (poll every 5 s instead) |
| Payment | One full payment per visit: Cash, Telebirr, CBE (reference required for the last two); Cashier verifies or rejects | Split bills, tips, tax or service charge, Telebirr/CBE API integration |
| Receipt | One receipt per verified payment, unique number, printable page | Thermal printer driver, reprint history |
| Operations | Docker Compose (API + PostgreSQL), README, demo seed data | CI pipeline, staging, backups, health checks, audit table, reports |

If your deadline is shorter than 15 days, keep the same order and use the cut list in the last section.

## Ground rules: build lean

Keep the existing 5-project solution, but fill it with the simplest code that works. Each swap below saves days without weakening the sale flow.

| The roadmap asks for | The lean plan does | Why it is safe for v1 |
| --- | --- | --- |
| MediatR + CQRS handlers, FluentValidation | One plain service per feature in Application (`MenuService`, `OrderService`…), DataAnnotations on request records | Same layers, a third of the files |
| Repository or port per use case | One `IAppDbContext` interface in Application, implemented by `RestaurantPosDbContext` | EF Core already is the repository |
| Identity schema + cookie + CSRF (5–10 days) | `PasswordHash` on `User`, `PasswordHasher<User>`, JWT bearer with a role claim (package already referenced) | About 1 day; switch to cookies in v2 |
| Same-transaction audit table | The who/when fields already on entities: `WaiterId`, `VerifiedByCashierId`, `VerifiedAt`, `CreatedAt` | Answers “who did it” for the demo |
| SignalR order hub | Kitchen and waiter pages poll every 5 s | No extra infrastructure; add the hub after v1 |
| Race tests, CI, staging, restore drills | Database unique indexes do the real protection; a manual demo script proves the flow | Constraints already block double opens and double receipts |

**Rules for every day**

1. Vertical slices: finish a feature from database to screen before starting the next.
2. One migration per phase at most; never edit `InitialCreate`.
3. The server calculates every price and total; Angular only displays them.
4. Timebox: when a task reaches 2× its hours, cut scope (last section) instead of polishing.
5. Test only rules that lose money or data: order totals, one open visit per table, one receipt per payment, role access.
6. Decide on Day 1, don’t wait. Write these 7 assumptions in the README ([decisions D-01–D-07](README.md#mvp-decisions-wbs-11)) and build on them: dine-in only; prices include tax; no modifiers; orders can’t be edited after sending; one full payment per visit; Telebirr and CBE need a reference; receipt number `R-YYYYMMDD-####`.

## Phases and milestones

Six phases, each closed by a gate you can demo. Don’t start a phase until the previous gate passes.

```text
Working day                 1  2  3  4  5  6  7  8  9 10 11 12 13 14 15
1 Foundation              █████████  ·  ·  ·  ·  ·  ·  ·  ·  ·  ·  ·  ·  18 h  Gate: DB migrates, shell routes
2 Auth and staff            ·  ·  ·██████  ·  ·  ·  ·  ·  ·  ·  ·  ·  ·  12 h  Gate: 4 roles log in, 403 works
3 Menu and tables           ·  ·  ·  ·  ·██████  ·  ·  ·  ·  ·  ·  ·  ·  12 h  Gate: a table opens only once
4 Orders and kitchen        ·  ·  ·  ·  ·  ·  ·█████████  ·  ·  ·  ·  ·  18 h  Gate: an order reaches Served
5 Bill, payment, receipt    ·  ·  ·  ·  ·  ·  ·  ·  ·  ·██████  ·  ·  ·  12 h  Gate: Verify issues one receipt
6 Integrate and ship        ·  ·  ·  ·  ·  ·  ·  ·  ·  ·  ·  ·██████  ·  12 h  Gate: demo script passes
Buffer                      ·  ·  ·  ·  ·  ·  ·  ·  ·  ·  ·  ·  ·  ·░░░   6 h  Overruns only, no new scope
                                                                    ▲ demo-ready at the end of Day 14
```

Phases 1 and 4 are the longest (18 h each): Foundation unblocks everything, and Orders holds the biggest screen.

## Work Breakdown Structure

25 work packages in 6 phases: 84 planned hours plus a 6-hour buffer. Work the rows top to bottom and set Status as you go — this table is your tracker.

| WBS | Work package | Done when | Day | Hours | Status |
| --- | --- | --- | --- | --- | --- |
| **1** | **Foundation** | **Gate:** a fresh DB migrates, tests are green, the Angular shell routes | D1–3 | **18** |  |
| 1.1 | Decisions note | The 7 assumptions are confirmed and linked from the root README (drafted in [docs/README.md](README.md#mvp-decisions-wbs-11)) | D1 | 1 | Not started |
| 1.2 | Domain fixes + order lines | `OrderItem` constructor and `ChangeName` fixed; `DomainException` replaces `IndexOutOfRangeException`; `Order` owns `Items`, `Total`, `MarkServed`; `OrderItem` stores `ItemName`; ~10 unit tests pass | D1–2 | 6 | Not started |
| 1.3 | EF config + migration | Explicit enum numbers; FKs; `numeric(12,2)` money; unique table number, email and receipt number; partial unique index for one non-closed visit per table; `PasswordHash` column; migration applies to a fresh DB | D2 | 4 | Not started |
| 1.4 | Backend skeleton | `IAppDbContext`, DI extension methods, ProblemDetails error handler (400/401/403/404/409), OpenAPI UI, NU1903 package bumped (no CORS: the dev proxy makes calls same-origin) | D2–3 | 3 | Not started |
| 1.5 | Angular shell | Tailwind, routes (login, app layout, not-found), HttpClient + dev proxy, header and sidebar, welcome page removed | D3 | 4 | Not started |
| **2** | **Auth and staff** | **Gate:** 4 roles log in; a Waiter calling a Manager endpoint gets 403 | D4–5 | **12** |  |
| 2.1 | Auth API | `POST /auth/login` returns a JWT (user id, role, 8 h expiry); `GET /auth/me`; inactive users rejected; dev Manager seeded | D4 | 4 | Not started |
| 2.2 | Angular auth | Login page, `AuthService` signal, bearer interceptor, 401 → login, `roleGuard`, sidebar per role | D4–5 | 4 | Not started |
| 2.3 | Employees | Manager lists, creates, changes role, deactivates; last active Manager protected; page works | D5 | 4 | Not started |
| **3** | **Menu and tables** | **Gate:** 6 items and 5 tables exist; opening the same table twice returns 409 | D6–7 | **12** |  |
| 3.1 | Menu API | Category and item CRUD, price, availability; `GET /menu` returns sellable items by category | D6 | 4 | Not started |
| 3.2 | Menu UI | Manager page: category list, item form, availability toggle | D6–7 | 3 | Not started |
| 3.3 | Tables API | Create and list tables; board with status; opening a visit marks the table Occupied in the same save | D7 | 2.5 | Not started |
| 3.4 | Table board UI | Colour-coded grid; open dialog with guest count → visit page | D7 | 2.5 | Not started |
| **4** | **Orders and kitchen** | **Gate:** an order goes Submitted → Preparing → Ready → Served at server prices | D8–10 | **18** |  |
| 4.1 | Order API | Send order with lines (server price + name snapshot); visit detail with running total; mark Served | D8 | 5 | Not started |
| 4.2 | Waiter order UI | Menu picker by category, cart with quantity and note, Send to kitchen, order status list | D9 | 6 | Not started |
| 4.3 | Kitchen API | Queue oldest first; Start and Ready actions | D10 | 2 | Not started |
| 4.4 | Kitchen board UI | New / Preparing / Ready columns, 5 s poll | D10 | 3 | Not started |
| 4.5 | Order tests | Totals; empty order rejected; illegal status jumps rejected | D8, D10 | 2 | Not started |
| **5** | **Bill, payment, receipt** | **Gate:** Verify closes the visit, frees the table and issues exactly one receipt | D11–12 | **12** |  |
| 5.1 | Bill API | Request bill; server bill = sum of line snapshots | D11 | 2 | Not started |
| 5.2 | Payment API | Record payment (amount from server); Cashier pending list; Verify = payment + receipt + close visit + free table in one `SaveChangesAsync`; Reject with reason | D11–12 | 5 | Not started |
| 5.3 | Cashier + receipt UI | Pending list, Verify / Reject, printable receipt (`@media print`) | D12 | 4 | Not started |
| 5.4 | Payment tests | Second Verify → 409 and no second receipt; reference rule for Telebirr/CBE | D12 | 1 | Not started |
| **6** | **Integrate and ship** | **Gate:** the demo script passes on a fresh database | D13–14 | **12** |  |
| 6.1 | End-to-end pass | Demo script run with 4 browser profiles; bugs fixed | D13 | 4 | Not started |
| 6.2 | UX states | Loading, empty and error states on every page; tablet width checked | D13–14 | 3 | Not started |
| 6.3 | Seed + README | Demo menu, tables and 4 accounts; run steps; demo script | D14 | 2 | Not started |
| 6.4 | Package | Angular build served by the API; Compose runs API + PostgreSQL; secrets from env vars | D14 | 3 | Not started |
| **7** | **Buffer** | Overruns only, no new scope | D15 | **6** |  |

## Build order

Build the phases in order, 1 → 6. Menu and Tables depend only on Auth, Orders need both, and Payment needs Orders. Inside every feature, follow the same 7-step recipe so each slice is usable before the next one starts.

**The slice recipe (every feature, every time)**

1. **Domain:** add or fix the entity method and its guard (throw `DomainException`); write the unit test for the rule.
2. **EF:** add `Infrastructure/Persistence/Configurations/<Entity>Configuration.cs`; add a migration only if the schema changed.
3. **Application:** `Features/<Feature>/<Feature>Service.cs` plus request and response records in `<Feature>Dtos.cs`. Use `IAppDbContext` and call `SaveChangesAsync` once per action.
4. **API:** a thin `Controllers/<Feature>Controller.cs` with `[Authorize(Roles = "...")]`, returning DTOs, never entities.
5. **Smoke:** call it from `RestaurantPOS.Api.http` — one happy path, one failure.
6. **Angular:** an API client in the scaffolded `core/services/<feature>.ts` (rename the class to `<Feature>Api`), then the page in `features/<feature>/pages/`, state in signals.
7. **Prove:** click through as the right role, then set the WBS row to Done.

Feature folders in Application: `Auth`, `Employees`, `Menu`, `Tables`, `Orders`, `Kitchen`, `Payments`.

**What each slice exposes**

| Slice | Endpoints (under `/api`) | Angular route | Role |
| --- | --- | --- | --- |
| Auth | `POST auth/login` · `GET auth/me` | `/login` | All |
| Employees | `GET, POST employees` · `PUT employees/{id}/role` · `POST employees/{id}/deactivate` | `/manager/employees` | Manager |
| Menu | `GET, POST categories` · `PUT categories/{id}` · `GET, POST items` · `PUT items/{id}` · `POST items/{id}/availability` · `GET menu` | `/manager/menu` | Manager; `GET menu`: Waiter |
| Tables | `GET, POST tables` · `GET tables/board` · `POST tables/{id}/sessions` | `/manager/tables`, `/waiter/tables` | Manager, Waiter |
| Orders | `GET sessions/{id}` · `POST sessions/{id}/orders` · `POST orders/{id}/served` | `/waiter/sessions/:id` | Waiter |
| Kitchen | `GET kitchen/orders` · `POST orders/{id}/start` · `POST orders/{id}/ready` | `/kitchen` | Kitchen |
| Bill and payment | `POST sessions/{id}/request-bill` · `GET sessions/{id}/bill` · `POST sessions/{id}/payments` · `GET payments/pending` · `POST payments/{id}/verify` · `POST payments/{id}/reject` | `/waiter/sessions/:id`, `/cashier` | Waiter, Cashier |
| Receipt | `GET receipts/{id}` | `/receipts/:id` | Cashier, Manager |

**Status rules to enforce in the Domain** (diagrams and invariants: [DOMAIN_MODEL.md](DOMAIN_MODEL.md))

- **Order:** Submitted → Preparing → Ready → Served. `Draft` and `Collected` stay unused in v1.
- **Visit (TableSession):** Open → BillRequested → PaymentPending → Closed; Reject sends it back to BillRequested.
- **Table:** Available ↔ Occupied, changed only in the same save as the visit.
- **Payment:** Pending → Verified or Rejected.

## Work plan: daily loop and weekly checkpoints

Work one WBS row at a time, in Day order. Measure progress against three weekly checkpoints, not against the full roadmap.

**Daily loop (about 6 focused hours)**

1. **Start, 10 min:** pick the first row not Done, set it to In progress, write today’s target in one line.
2. **Build, two 2.5 h blocks:** follow the slice recipe; write the rule test first when money or status is involved.
3. **Prove, 30 min:** click through the slice as its role and run `dotnet test`.
4. **Close, 10 min:** commit with the WBS code in the message (for example `4.1 send order API`), set the row to Done, note any blocker in the row.

**Weekly checkpoints**

| Checkpoint | End of | Must be true | If it isn’t |
| --- | --- | --- | --- |
| Week 1 | Day 5 | Phases 1–2 done: fresh DB migrates, 4 roles log in, 403 works | Take cuts 2 and 4 now (seed staff and tables) |
| Week 2 | Day 10 | Phases 3–4 done: an order reaches Served | Take cuts 3, 5 and 6 |
| Week 3 | Day 14 | Phases 5–6 done: the demo script passes | Use the Day 15 buffer; take cuts 1 and 7 |

## Definition of done and demo script

A WBS row is Done only when its slice works in the browser for the right role. The MVP is done when the demo script below runs clean on a fresh database.

**Every row is Done when**

- It works end to end — database, API, screen — as the right role.
- A wrong role gets 403 and bad input gets a readable 400 message.
- Rule tests for money or status pass (`dotnet test`).
- Angular holds no hard-coded prices or totals.
- It is committed with its WBS code in the message.

**Demo script (the MVP acceptance test, about 10 minutes)**

- [ ] Fresh DB: `docker compose up`; the API applies migrations and seeds the Manager
- [ ] Manager logs in and creates Waiter, Kitchen and Cashier accounts
- [ ] Manager adds 2 categories, 6 items and 5 tables, and marks one item unavailable
- [ ] Waiter opens Table 3 for 4 guests; opening Table 3 again shows “already open”
- [ ] Waiter sends 2 orders; the unavailable item can’t be picked
- [ ] Kitchen moves both orders to Ready; Waiter marks them Served
- [ ] Manager changes a price; the bill still shows the old price for lines already sent
- [ ] Waiter requests the bill and records a Telebirr payment with a reference
- [ ] Cashier rejects it once (wrong reference); Waiter records it again; Cashier verifies
- [ ] The receipt prints with its number, and Table 3 is Available again
- [ ] Clicking Verify twice does not create a second receipt
- [ ] Waiter can’t open `/manager/menu` or call its API (403)

**Before you hand it over (Day 14)**

- [ ] Connection string and JWT signing key come from environment variables, not committed files
- [ ] README has run steps, demo accounts, the 7 assumptions and the v2 list
- [ ] `dotnet test` and `ng build` pass on a clean clone

## If time runs out: cut list and risks

Cut in this order, top first. Taking every cut saves about 16.5 hours — roughly a 12-day plan including the buffer. Never cut the sale flow itself: login and role checks, server-side prices, one open visit per table, atomic Verify, and the receipt.

| # | Cut | Saves | Do this instead |
| --- | --- | --- | --- |
| 1 | Docker packaging (6.4) | 3 h | Demo locally with `dotnet run` and `ng serve` |
| 2 | Employees API and page (2.3) | 3 h | Seed the 4 accounts in a `DbSeeder` (about 1 h) |
| 3 | Menu manager page and CRUD (3.1, 3.2) | 5 h | Seed the menu; keep `GET menu`; change prices through the OpenAPI UI |
| 4 | Manager tables page (part of 3.3) | 1 h | Seed 5 tables |
| 5 | Polished UX states (6.2) | 2 h | One global error toast in the HTTP interceptor |
| 6 | Kitchen board columns (4.4) | 1.5 h | One list with a next-status button |
| 7 | Payment Reject flow (part of 5.2, 5.3) | 1 h | Verify only; the Cashier asks the Waiter to re-record |

If you take cuts 2–4, change the matching demo-script steps to “seeded”.

| Risk | Early warning | Fallback |
| --- | --- | --- |
| Auth takes more than its 8 hours | Day 4 ends without a working token | Keep JWT + seeded users; finish the guard and sidebar later; don’t switch to cookies |
| Migration fails (partial index, old local data) | `dotnet ef database update` fails on Day 2 | If only your local dev DB holds data, reset it with `docker compose down -v` and migrate again |
| The waiter order screen (biggest page) overruns | Day 9 ends without Send to kitchen working | Plain form: item dropdown + quantity + Add; style it on Day 13 |
| Scope creep (modifiers, tips, SignalR, reports) | You start anything not in the WBS | Add it to the README v2 list and return to the current row |
| Owner changes payment rules mid-build | A request for tax, split bill or tips | The Day 1 assumptions stand for v1; the change goes to v2 |

The original 56-task roadmap stays your v2 backlog: cookie auth with CSRF, audit table, SignalR, CI, staging and backups, search, shift reports, and payment-provider integration.
