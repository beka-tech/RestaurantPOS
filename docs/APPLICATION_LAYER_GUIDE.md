# Build the Application layer yourself: MediatR + CQRS

This is a workbook for your existing Restaurant POS project. You write the implementation; this guide gives you the order, contracts, small syntax examples, and checks for each exercise. Complete one milestone before moving to the next.

Your first goal is simple: **create a menu category, then query the categories**. After that, build menu items and state-changing commands using the same pattern.

## 1. Your starting point

The repository currently has:

- .NET 10 projects with the Application project referencing Domain.
- MediatR `14.2.0`, FluentValidation `12.1.1`, and its dependency injection extensions already referenced in [RestaurantPOS.Application.csproj](../backend/src/RestaurantPOS.Application/RestaurantPOS.Application.csproj).
- Domain entities with constructors and methods that enforce some business rules.
- An EF Core [RestaurantPosDbContext](../backend/src/RestaurantPOS.Infrastructure/Persistence/RestaurantPosDbContext.cs) in Infrastructure and an initial PostgreSQL migration.
- An API that registers the DbContext, but does not yet register Application handlers or expose feature controllers.
- An xUnit test project referencing Application and Domain. Its current `UnitTest1` is an empty placeholder.

You can complete the Application exercises with test fakes before touching PostgreSQL or HTTP. The later integration exercises explain how to connect your finished use cases.

For running the existing project and database, use the [startup README](../Readme.md).

## 2. Understand the responsibilities

| Layer | Its job in this project | Example |
| --- | --- | --- |
| Domain | Protect an entity's valid state | A category cannot have a blank name |
| Application | Coordinate one use case | Create a category and request that it be saved |
| Infrastructure | Implement access to outside systems | Persist the category with EF Core/PostgreSQL |
| API | Translate HTTP into a use case and its response into HTTP | Receive a POST and send a command |

Application owns commands, queries, handlers, validators, DTOs, and interfaces describing the persistence it needs. It should not know a connection string, `DbContext`, `DbSet`, SQL, `HttpContext`, controllers, or HTTP status codes in the design used here.

Project references should remain:

```text
Application    -> Domain
Infrastructure -> Application + Domain
API            -> Application + Infrastructure
```

At runtime, a handler calls an Application interface. Dependency injection supplies an Infrastructure implementation. Calling that implementation does not require Application to reference Infrastructure.

**Explain before coding:** Why can the handler use a repository interface without knowing EF Core?

### CQRS in this project

CQRS separates requests that change state from requests that read data. Use one PostgreSQL database with different write and read paths; separate databases and event sourcing are unnecessary for these exercises. This is a supported basic CQRS arrangement. [Microsoft CQRS guidance](https://learn.microsoft.com/en-us/azure/architecture/patterns/cqrs)

| Type | Purpose | Your first example | Result |
| --- | --- | --- | --- |
| Command | Perform a change | `CreateMenuCategoryCommand` | The new category's `Guid` |
| Query | Read data without changing it | `GetMenuCategoriesQuery` | A list of DTOs |
| Handler | Execute one request | `CreateMenuCategoryCommandHandler` | A task containing the request's result |
| DTO | Describe returned data | `MenuCategoryDto` | Plain data, without entity mutation methods |
| Behavior | Run shared work around a handler | `ValidationBehavior<TRequest, TResponse>` | Reject invalid input before execution |

A command can return an identifier or confirmation. A query must not add, update, or save entities.

MediatR dispatches messages inside the .NET process. Your command/query names express CQRS intent; MediatR itself does not enforce that a query is read-only. [MediatR 14.2.0 reference](https://github.com/LuckyPennySoftware/MediatR/blob/v14.2.0/README.md)

```text
API or test -> ISender.Send(request, token)
            -> validation behavior
            -> request handler
                 command: domain entity -> repository -> unit of work
                 query:   read service -> DTOs
            -> result returned to caller
```

## 3. Work in small milestones

| Milestone | What you build | Evidence you are ready to continue |
| --- | --- | --- |
| 1 | DTO, requests, persistence interfaces | Application compiles without referencing EF Core |
| 2 | Category validator | Valid and invalid inputs have meaningful tests |
| 3 | Create-category handler | Works with fakes and saves before returning the ID |
| 4 | List-categories handler | Reads through a read interface and never saves |
| 5 | Validation behavior and registration | Invalid `ISender.Send` never reaches the handler |
| 6 | Menu-item command | Missing and inactive categories are handled |
| 7 | Optional Infrastructure/API integration | POST then GET proves a real database write |

Run commands below from the repository root:

```powershell
dotnet restore backend/src/RestaurantPOS.sln
dotnet build backend/src/RestaurantPOS.Application/RestaurantPOS.Application.csproj
dotnet test backend/src/RestaurantPOS.Tests/RestaurantPOS.Tests.csproj
```

No package installation is needed to start: the requested Application packages are already in your project. Keep those versions while following this workbook. Milestone 5 adds a test-only package for building a standalone dependency injection provider.

MediatR 14 includes license-key configuration. Consult the [publisher's setup instructions](https://github.com/LuckyPennySoftware/MediatR/blob/v14.2.0/README.md#setting-the-license-key) for your usage. If you configure a key, load it through configuration or the documented `MEDIATR_LICENSE_KEY` environment variable rather than committing it.

## 4. Plan the folders

Create files as you reach their exercises. This is a target layout, not an inventory of files already present:

```text
RestaurantPOS.Application/
|-- Abstractions/
|   `-- Persistence/
|       |-- IMenuCategoryRepository.cs
|       |-- IMenuCategoryReadService.cs
|       |-- IMenuItemRepository.cs          (milestone 6)
|       `-- IUnitOfWork.cs
|-- Common/
|   |-- Behaviors/
|   |   `-- ValidationBehavior.cs
|   `-- Exceptions/
|       |-- EntityNotFoundException.cs      (milestone 6)
|       `-- BusinessRuleViolationException.cs
|-- Features/
|   `-- Menu/
|       |-- Categories/
|       |   |-- MenuCategoryDto.cs
|       |   |-- Commands/
|       |   |   `-- CreateMenuCategory/
|       |   |       |-- CreateMenuCategoryCommand.cs
|       |   |       |-- CreateMenuCategoryCommandValidator.cs
|       |   |       `-- CreateMenuCategoryCommandHandler.cs
|       |   `-- Queries/
|       |       `-- GetMenuCategories/
|       |           |-- GetMenuCategoriesQuery.cs
|       |           `-- GetMenuCategoriesQueryHandler.cs
|       `-- Items/
|           `-- Commands/
|               `-- CreateMenuItem/         (milestone 6)
`-- DependencyInjection.cs
```

Use namespaces matching these folders, starting with `RestaurantPOS.Application`. Use `public` request, handler, and validator types for straightforward assembly scanning.

Keep each feature together. Add a new abstraction when a use case needs it instead of designing repositories for every entity upfront.

## 5. Milestone 1: define requests and boundaries

Read the actual [MenuCategory](../backend/src/RestaurantPOS.Domain/Entities/MenuCategory.cs) constructor first. Its parameter order is `(name, displayOrder, description)`. The base [Entity](../backend/src/RestaurantPOS.Domain/Entity.cs) identifier is **`ID`**, not `Id`.

### Your tasks

1. Define `MenuCategoryDto` as a record with `Id`, `Name`, `Description`, `DisplayOrder`, and `IsActive`. `Description` is nullable. Its `Id` maps from the entity's `ID`.
2. Define an immutable create command with `Name`, `DisplayOrder`, and optional `Description`. Use `IRequest<Guid>` from namespace `MediatR`.
3. Define the list query. Give it `IncludeInactive`, defaulting to `false`, and response type `IReadOnlyList<MenuCategoryDto>`.
4. Write the persistence interfaces described below in Application.

Small syntax reference for the command:

```csharp
using MediatR;

public sealed record CreateMenuCategoryCommand(
    string Name,
    int DisplayOrder,
    string? Description = null) : IRequest<Guid>;
```

Add your namespace. Write the DTO and query yourself using the same record syntax.

### Persistence contracts to implement yourself

| Interface | Member | Meaning |
| --- | --- | --- |
| `IMenuCategoryRepository` | `void Add(MenuCategory category)` | Stage a new entity; do not save here |
| `IMenuCategoryRepository` | `Task<MenuCategory?> GetByIdAsync(Guid id, CancellationToken cancellationToken)` | Load an entity for a command; return null when absent |
| `IUnitOfWork` | `Task<int> SaveChangesAsync(CancellationToken cancellationToken)` | Commit staged changes; return the persistence adapter's affected-entry count |
| `IMenuCategoryReadService` | `Task<IReadOnlyList<MenuCategoryDto>> GetListAsync(bool includeInactive, CancellationToken cancellationToken)` | Return the filtered, sorted projection |

Import `RestaurantPOS.Domain.Entities` for `MenuCategory` and your DTO namespace for the read service. Do not expose `IQueryable` or EF types through these interfaces.

For the read-service contract, define sorting as `DisplayOrder`, then `Name`, then entity `ID`. Exclude inactive categories unless requested. Return an empty list when there are no matches.

`IUnitOfWork` here is a narrow abstraction over the shared context's save operation. A repository stages changes; the handler chooses when to save. This allows a later command to stage related changes and commit them together.

**Checkpoint:** Application builds. You can explain why a repository's `Add` does not also call `SaveChangesAsync`.

## 6. Milestone 2: write the category validator

Create a class deriving from `AbstractValidator<CreateMenuCategoryCommand>`. Add rules for:

- A required, nonblank `Name`.
- A `DisplayOrder` greater than or equal to zero.
- A nullable `Description`, with no invented mandatory requirement.

Hint: start with `RuleFor`, `NotEmpty`, and `GreaterThanOrEqualTo`. Do not add uniqueness or length limits without deciding those business requirements first.

There are three different kinds of checks:

| Check | Place it here | Reason |
| --- | --- | --- |
| Request has a blank name | Validator | Reject malformed input before use-case execution |
| Entity cannot exist with a blank name | Domain constructor | Protect state even when created outside MediatR |
| Referenced category exists | Handler using a persistence interface | Requires current stored data |

The overlap between input validation and domain guards is deliberate. A domain entity should remain valid when used by a test, import job, or another caller.

### Write tests before moving on

| Input | Expected |
| --- | --- |
| `Name = "Drinks", DisplayOrder = 0` | Valid |
| Empty name | Name failure |
| Whitespace-only name | Name failure |
| Negative display order | DisplayOrder failure |
| `Description = null` | Valid |
| Name with leading/trailing spaces | Valid; constructor will trim it later |

Use real validators in tests. You may use `ValidateAsync` and xUnit assertions, or `TestValidateAsync` from `FluentValidation.TestHelper`; the test helper is supplied by FluentValidation. [Validator testing guidance](https://docs.fluentvalidation.net/en/latest/testing.html)

**Checkpoint:** Your validator tests fail if you remove either required rule. No database is needed.

## 7. Milestone 3: write the create-category handler

The handler implements `IRequestHandler<CreateMenuCategoryCommand, Guid>` and receives `IMenuCategoryRepository` and `IUnitOfWork` through its constructor.

Its method shape is:

```csharp
public Task<Guid> Handle(
    CreateMenuCategoryCommand request,
    CancellationToken cancellationToken)
```

Use `async` when you implement its awaited work. The request's response type and the handler's second generic argument must match. [MediatR handler contract](https://github.com/LuckyPennySoftware/MediatR/blob/v14.2.0/src/MediatR/IRequestHandler.cs)

### Write the method from this algorithm

1. Respect cancellation before staging work.
2. Construct a `MenuCategory` using the request and the constructor's actual parameter order.
3. Stage it through the repository.
4. Await the unit of work's save, passing the supplied token.
5. Return the entity's `ID` after the save succeeds.

Let the entity trim its name and description. Do not assign its private setters or return its identifier before saving. Do not catch every exception and turn it into an empty GUID.

### Build your own test fakes

In the Tests project, write a fake repository that records added categories and can find a configured category. Write a fake unit of work that records saves, captures the token, and can be configured to throw.

These fakes test Application behavior. They do not prove EF mapping, real transactions, or database durability.

Test these observable outcomes:

- A valid request stages one category with the trimmed name, correct order, and active state.
- The returned nonempty GUID is the staged entity's `ID`.
- Saving happens before a result is returned.
- The caller's cancellation token reaches `SaveChangesAsync`.
- If saving fails, the operation fails rather than returning success.
- An already-cancelled token prevents staging and saving.

To prove the handler waits for saving, let your fake save return a task controlled by `TaskCompletionSource<int>`. Verify that the handler task is incomplete until you complete that save task, then assert the returned ID.

A direct call to `handler.Handle` does **not** invoke a MediatR behavior. For now, test malformed input through the validator and valid input through the handler. You will test the complete dispatch path in milestone 5.

**Checkpoint:** This use case works with fakes while PostgreSQL is stopped.

## 8. Milestone 4: write the list query

Implement `GetMenuCategoriesQueryHandler` with response type `IReadOnlyList<MenuCategoryDto>`. Inject only `IMenuCategoryReadService`.

Your handler should pass `IncludeInactive` and the cancellation token to the read service, await its result, and return it. It should not use a write repository or a unit of work.

The Infrastructure adapter will own filtering, ordering, and DTO projection. This keeps EF expressions outside Application while allowing the read side to retrieve only the fields it needs.

Test that the handler:

- Forwards `false` for the default query and `true` when requested.
- Passes the cancellation token.
- Returns the read service's DTOs or an empty list.
- Does not mutate or save anything.

Test the real filtering and sorting later against the Infrastructure adapter. A fake that returns sorted data cannot prove the database query sorts correctly.

**Checkpoint:** You have one command path and one query path, with distinct contracts.

## 9. Milestone 5: build the validation behavior

Now connect validators to every dispatched request through `ValidationBehavior<TRequest, TResponse>`, implementing `IPipelineBehavior<TRequest, TResponse>` with `where TRequest : notnull`.

Inject `IEnumerable<IValidator<TRequest>>`. Your behavior's method shape for MediatR 14.2.0 is:

```csharp
public async Task<TResponse> Handle(
    TRequest request,
    RequestHandlerDelegate<TResponse> next,
    CancellationToken cancellationToken)
```

The parameter order is `request`, `next`, then `cancellationToken`. The continuation accepts a token; continue with `await next(cancellationToken)` after validation. [MediatR 14.2.0 behavior contract](https://github.com/LuckyPennySoftware/MediatR/blob/v14.2.0/src/MediatR/IPipelineBehavior.cs)

### Your implementation algorithm

1. Create a `ValidationContext<TRequest>` for the request.
2. Await each validator's `ValidateAsync`, passing the token.
3. Gather its `ValidationFailure` objects, preserving field names and messages.
4. If there are failures, throw `FluentValidation.ValidationException` containing them.
5. Otherwise call the next delegate exactly once and return its result.

When there are no validators, continue to the handler. Do not swallow cancellation.

Use asynchronous validation consistently: it supports both synchronous and asynchronous rules. [FluentValidation async guidance](https://docs.fluentvalidation.net/en/latest/async.html)

For this first behavior, await validators sequentially. If a future validator uses the request's DbContext, parallel validation can issue overlapping operations on the same context, which EF Core does not support. [DbContext threading guidance](https://learn.microsoft.com/en-us/ef/core/dbcontext-configuration/#avoiding-dbcontext-threading-issues)

### Register what you built

Create an `AddApplication(this IServiceCollection services)` extension method in `DependencyInjection.cs`, returning the service collection. Use `using Microsoft.Extensions.DependencyInjection;` and `using FluentValidation;`.

Inside it, register MediatR using the **Application assembly**, identified by one of your request types. These are wiring hints, not a complete method:

```csharp
services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssemblyContaining<CreateMenuCategoryCommand>();
    cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
});

services.AddValidatorsFromAssemblyContaining<CreateMenuCategoryCommandValidator>();
```

Add the namespaces for your types. MediatR's package supplies its DI registration; an older standalone MediatR DI package is unnecessary. [Versioned registration reference](https://github.com/LuckyPennySoftware/MediatR/blob/v14.2.0/README.md#registering-with-iservicecollection)

FluentValidation's scanning extension is available through your existing dependency injection extensions package. Public validators are discovered and registered as scoped by default. [FluentValidation DI reference](https://docs.fluentvalidation.net/en/latest/di.html)

### Test the actual dispatch path

For standalone provider tests, add the concrete logging implementation to the **Tests** project:

```powershell
dotnet add backend/src/RestaurantPOS.Tests/RestaurantPOS.Tests.csproj package Microsoft.Extensions.Logging --version 10.0.0
```

This supplies `AddLogging` and brings the concrete dependency injection provider transitively. The Application packages currently supply only the corresponding abstractions. [Microsoft logging package dependencies](https://www.nuget.org/packages/Microsoft.Extensions.Logging/10.0.0#dependencies-body-tab)

Create a `ServiceCollection` in a test, add logging, call your `AddApplication`, and register your fakes as implementations of the persistence interfaces. Build the provider, create a scope, and resolve `ISender` from that scope.

Send a command rather than constructing its handler directly:

```csharp
var id = await sender.Send(
    new CreateMenuCategoryCommand("Drinks", 1), cancellationToken);
```

Write tests proving:

- A valid command reaches its handler and returns the saved entity's ID.
- An invalid command throws a validation exception and never stages or saves.
- A query with no validator still executes.
- Invalid input with two broken rules exposes both failures.
- A behavior test's next delegate runs once on success and zero times on failure.

Use `services.AddLogging()` in the test composition root; a manually created provider does not have all the registrations supplied by an ASP.NET host.

**Checkpoint:** You can follow request -> behavior -> handler with breakpoints and explain every dependency.

## 10. Milestone 6: create a menu item

Apply the pattern yourself to `CreateMenuItemCommand`, its validator, and handler. Return a GUID again.

Request fields: `MenuCategoryId`, `Name`, nullable `Description`, and decimal `Price`.

Read the [MenuItem constructor](../backend/src/RestaurantPOS.Domain/Entities/MenuItem.cs): `(menuCategoryId, name, description, price)`. It allows a price of **zero**. Validate `Price >= 0`, not `Price > 0`, unless you deliberately change that rule throughout the project.

### New pieces to design

- `IMenuItemRepository` with `void Add(MenuItem item)`, staging without saving.
- `EntityNotFoundException` for a missing referenced entity, carrying a useful identifier.
- `BusinessRuleViolationException` for an explicitly rejected use-case condition.

For this exercise, adopt the policy: **new items may only be added to an active category**. This is a proposed application policy, not a rule already enforced by the MenuItem constructor.

### Handler algorithm

1. Load the referenced category through `IMenuCategoryRepository.GetByIdAsync`.
2. Report not found if it does not exist.
3. Reject an inactive category with your explicit business-rule exception.
4. Construct the item using its domain constructor.
5. Stage the item, then save once through the shared unit of work.
6. Return the item's `ID` after saving.

Do not trust a nonempty GUID to prove a category exists. The current DbContext has no explicit category relationship, and the initial migration has no foreign-key constraints enforcing these identifier references. Treat database relationship configuration as a later Infrastructure exercise.

### Acceptance cases

| Scenario | Expected |
| --- | --- |
| Active category, valid name, price 250 | Item staged and saved; new ID returned |
| Price zero | Valid |
| Negative price | Validation failure before handler |
| Empty category GUID | Validation failure before handler |
| Category not found | Not-found exception; no item staged or saved |
| Category inactive | Business-rule exception; no item staged or saved |
| Saving fails | Failure propagated; no successful response |

The lookup is a use-case check, not complete concurrency protection. A future design must decide what happens if a category is deactivated between lookup and save.

**Checkpoint:** You can add this feature without changing the category-create handler or the validation behavior.

## 11. Optional integration: make the use cases persist

Your Application milestone is complete when the use cases, validation pipeline, registrations, and tests work with fakes. Connecting the real database requires implementations in **Infrastructure**, not more EF code in Application.

Build these adapters yourself:

| Adapter | Implements | Work to do |
| --- | --- | --- |
| `MenuCategoryRepository` | `IMenuCategoryRepository` | Stage with `MenuCategories.Add`; load by `ID` with cancellation |
| `MenuItemRepository` | `IMenuItemRepository` | Stage with `MenuItems.Add` |
| `MenuCategoryReadService` | `IMenuCategoryReadService` | Filter active when required, sort, project to DTOs, materialize asynchronously |
| `EfUnitOfWork` | `IUnitOfWork` | Delegate to the shared context's `SaveChangesAsync` |

Use a no-tracking read query. For writes that later update existing entities, load tracked entities, call their domain methods, and save.

Register all adapters as **scoped**. They must receive the same scoped `RestaurantPosDbContext`. Keep the API's existing `AddDbContext` setup, or move it into an Infrastructure registration extension once; do not configure two separate contexts for one use case.

For now, the command handler explicitly saves. If you later introduce a transaction or unit-of-work behavior, make the save ownership explicit so one command is not saved in two places.

Verify filtering and sorting with real PostgreSQL integration tests or reproducible manual requests. Unit tests with fakes cannot validate EF translation.

## 12. Optional integration: send requests from the API

In the API composition root, call your `AddApplication()` and register the real persistence adapters. A controller should inject `ISender`, map input to a request, await `Send` with the HTTP cancellation token, and translate the result.

Plan these two endpoints first; you still have to implement them:

| Endpoint | Application request | Successful response |
| --- | --- | --- |
| `POST /api/menu-categories` | `CreateMenuCategoryCommand` | `201 Created` with the new ID |
| `GET /api/menu-categories?includeInactive=false` | `GetMenuCategoriesQuery` | `200 OK` with DTOs |

If you add a `Location` header to the POST response, make it point to a GET endpoint you actually implement.

### Keep HTTP errors at the boundary

Use the exception convention from this workbook consistently. Add an API exception handler/middleware that translates known failures into Problem Details:

| Application outcome | Planned HTTP mapping |
| --- | --- |
| `FluentValidation.ValidationException` | `400` with field validation errors |
| Your `EntityNotFoundException` | `404` |
| Your `BusinessRuleViolationException` | `409` |
| Unexpected failure | `500` with internal details logged |

Without this API handling, an unhandled validation exception can become a `500`. Registering validators alone does not create HTTP error responses. Do not map every `ArgumentException` or `InvalidOperationException` to a client error; infrastructure defects can throw those types too.

Authentication and authorization are a separate milestone. These early endpoints are local learning exercises, not a completed employee-permission model. Continue with the [Identity, RBAC, and ABAC workbook](AUTHENTICATION_AUTHORIZATION_GUIDE.md) to build login, current-user mapping, role policies, and resource permissions. Obtain employee identity from a trusted current-user abstraction rather than accepting a caller's arbitrary user ID.

### Manual end-to-end check

Start PostgreSQL, apply migrations, and run the API using the [README](../Readme.md). In an HTTP client, POST:

```json
{
  "name": "Drinks",
  "displayOrder": 1,
  "description": "Hot and cold drinks"
}
```

GET the category list and find the returned ID. Then POST a blank name and confirm a `400` with no new row. Restart the API and GET again to confirm the category persists.

## 13. Continue with real business commands

After menu creation works, build these in order. Each row is a new exercise, not a generated implementation:

| Next use case | Domain operation to inspect | What the handler coordinates |
| --- | --- | --- |
| Change item price | `MenuItem.ChangePrice` | Load item, report absence, change price, save |
| Make item unavailable | `MenuItem.MakeUnavailable` | Load item, call method, save |
| Deactivate category | `MenuCategory.Deactivate` | Load category, call method, save; decide the policy for existing items |
| Open table session | `RestaurantTable` and `TableSession` | Validate actor/table, create session, occupy table, save together |
| Submit order | `Order.Submit` | Validate the session and order contents, call method, save |
| Prepare/ready order | Current `Order` transition methods | Validate actor/state, transition, save |
| Verify payment | `Payment.Verify` | Validate cashier/payment/session, coordinate receipt and closure as specified |

Name commands after an action such as `SubmitOrderCommand`, rather than letting clients set an arbitrary status. Keep entity state transitions in Domain, orchestration in Application, and storage mechanics in Infrastructure.

### Read the code before implementing later workflows

[ENTITIES.md](ENTITIES.md) and [DOMAIN_MODEL.md](DOMAIN_MODEL.md) describe intended behavior beyond the current source. Resolve these known differences yourself before building on them:

| Current code fact | Why it matters |
| --- | --- |
| `MenuCategory.ChangeName` validates but never assigns the new name or marks it updated | Fix and test the entity method before adding a rename command |
| `OrderItem` checks its unassigned `OrderId` property instead of constructor parameter `orderId` | Every current construction fails; fix and test this guard first |
| Current order names include `Submit`, `Submitted`, and `MarkRead` | Older docs use `Confirm`, `Confirmed`, and `MarkReady`; choose consistent contracts before adding handlers |
| Order has no item collection or total yet | Decide and implement aggregate behavior before promising order-content checks |
| Current `TableStatus` is only `Available` / `Occupied` | Bill/payment lifecycle is modeled on TableSession; do not invent enum members in handlers |
| Electronic-payment reference requirements in the docs are not fully guarded in Domain | Agree on and test the rule before payment handlers |

Write failing domain tests first for those issues, then make the fixes yourself. Do not work around broken entity behavior with reflection or public setters.

For multi-entity workflows, also decide transaction boundaries, database constraints, concurrency handling, and duplicate-request behavior. For example, two simultaneous requests must not open two active sessions for one table; two payment-verification requests must not create two receipts. A validation query alone does not guarantee that.

## 14. When something does not work

| Symptom | Check |
| --- | --- |
| No handler is registered | Scan Application's assembly, confirm the handler's request/response types, and call `AddApplication` |
| A handler dependency cannot be resolved | Register its persistence interface implementation in the composition root |
| Validator never runs | Confirm validator scanning and behavior registration; direct `Handle` bypasses the pipeline |
| Behavior signature does not compile | Use the version 14 parameter order in milestone 5 |
| `AddValidatorsFromAssemblyContaining` is missing | Check the existing DI extensions package and `using FluentValidation` |
| Scoped-service resolution error in a test | Create a scope and resolve `ISender` inside it |
| Logging-service error in a test | Add logging to the test service collection |
| HTTP validation errors become `500` | Implement the API's exception-to-response mapping |
| Command returns an ID but no row exists | Confirm staging and awaiting save on the same scoped DbContext |
| EF reports another operation already running | Await operations on a shared context sequentially |

## 15. Your first work session

Work only on milestones 1-3 to begin. Your deliverables are:

- [ ] A category DTO, create command, and list query.
- [ ] The three category/persistence interfaces.
- [ ] A category validator and its input tests.
- [ ] A create-category handler.
- [ ] Test fakes and meaningful handler tests.
- [ ] A passing Application build and test run.

Then explain, without reading the guide: **What does the command describe? What does its handler coordinate? What does the entity protect? Who actually writes to PostgreSQL?**

If you can answer those four questions and your checks pass, move on to the query and MediatR pipeline.
