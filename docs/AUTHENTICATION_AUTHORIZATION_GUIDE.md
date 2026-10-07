# Build authentication and authorization yourself

This workbook continues the [Application-layer guide](APPLICATION_LAYER_GUIDE.md). You implement the code; the sections below give you a design, exercises, small syntax references, and acceptance checks for ASP.NET Core Identity, RBAC, and ABAC.

Your first goal: **sign in an employee and protect a manager-only menu command**. Your second goal: **allow a waiter to submit only their own draft order in their own open session**.

Work through the milestones in order. Authentication spans Infrastructure and API as well as Application; it cannot be completed entirely inside the Application project.

## 1. Know what each term means

| Term | Question it answers | POS example |
| --- | --- | --- |
| Authentication | Who is calling? | This request belongs to a signed-in employee |
| Authorization | Is that caller allowed to do this? | This employee may change a menu price |
| Identity | How do we manage accounts and sign-in? | Password verification, account lockout, roles, cookies |
| RBAC: role-based access control | Does the caller have an allowed role? | A Cashier can verify payments |
| ABAC: attribute-based access control | Do the caller, resource, and current conditions satisfy a rule? | A Waiter can submit an order they own while its session is open |
| Claim | What information does the authenticated principal carry? | Identity account ID and role names |
| Policy | Which requirements must pass for an operation? | Authenticated, active employee, correct role, permitted resource |

RBAC and ABAC can work together. A role answers part of the permission question; ownership, active state, and resource state answer the rest.

Identity manages accounts, passwords, roles, and related account features. Use its managers for credential operations rather than adding password hashing to Domain entities. [Identity introduction](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/identity?view=aspnetcore-10.0)

## 2. Your actual starting point

The backend currently targets .NET 10 and already references:

- `Microsoft.AspNetCore.Identity.EntityFrameworkCore` `10.0.12` in [Infrastructure](../backend/src/RestaurantPOS.Infrastructure/RestaurantPOS.Infrastructure.csproj).
- `Microsoft.AspNetCore.Authentication.JwtBearer` `10.0.12` in [API](../backend/src/RestaurantPOS.Api/RestaurantPOS.Api.csproj).

However, [Program.cs](../backend/src/RestaurantPOS.Api/Program.cs) does not register Identity or authentication and does not call `UseAuthentication`. A package reference alone does not secure an endpoint.

Your [Domain User](../backend/src/RestaurantPOS.Domain/Entities/User.cs) is an employee profile. It contains names, email, one role, and `IsActive`; it does not contain a password hash or login implementation. Its identifier is `ID`, inherited from [Entity](../backend/src/RestaurantPOS.Domain/Entity.cs).

The existing [UserRole enum](../backend/src/RestaurantPOS.Domain/Enums/UserRole.cs) has exactly:

```text
Waiter
Kitchen
Cashier
Manager
```

Use those role names consistently. `Admin` is not currently a project role. A Manager has only the permissions you explicitly grant; there is no automatic role hierarchy.

## 3. Choose the first authentication flow

For these exercises, use **ASP.NET Core Identity with an HttpOnly authentication cookie**, custom controller endpoints, and a same-origin Angular-to-API connection through a development proxy.

This fits the project's browser application and avoids building a token issuer just to learn permissions. Microsoft recommends cookies for browser-based Identity clients. [Identity for SPA backends](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/identity-api-authorization?view=aspnetcore-10.0)

| Approach | What authenticates the API call? | When to explore it |
| --- | --- | --- |
| This workbook | Identity's application cookie | First browser POS exercise |
| Identity API endpoints | Identity cookies or its built-in bearer tokens | Alternative endpoint implementation |
| OAuth/OIDC provider + JWT bearer validation | Standards-based access token from a provider | Separate clients, mobile apps, SSO, or distributed APIs |

Identity API bearer tokens are **not JWTs** and cannot be validated by `AddJwtBearer`. `MapIdentityApi` is not a general OAuth/OIDC server. The existing JwtBearer reference does not force you to use JWTs. [Identity token explanation](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/identity-api-authorization?view=aspnetcore-10.0#tokens)

For a later JWT track, use a suitable OAuth/OIDC provider and validate the access token's issuer, audience, lifetime, and signature in the API. Follow the provider's supported flow instead of minting a homemade production JWT from a password endpoint. [JWT bearer guidance](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/configure-jwt-bearer-authentication?view=aspnetcore-10.0)

## 4. Decide the boundaries and account mapping

| Project | Pieces you will build |
| --- | --- |
| Domain | Existing employee and business entities; lifecycle invariants |
| Application | Login/current-user contracts where needed, current employee abstraction, permission interfaces, commands, authorization exceptions |
| Infrastructure | Identity account class/context, Identity-backed account services, employee-account mapping, persistence |
| API | Cookie configuration, current principal adapter, policies and resource authorization handlers, auth controllers, antiforgery integration |
| Tests | Permission unit tests and real authentication/API integration tests |

Application must not take dependencies on `IdentityUser`, `UserManager`, `SignInManager`, `HttpContext`, or ASP.NET authorization-handler types. Use your own interfaces at that boundary.

### Keep credentials and employees distinct

For this workbook, choose a separate Identity account with an explicit employee link:

```text
Identity account
  Id: Guid
  EmployeeId: Guid  ---------> Domain.User.ID
  credential/lockout data       employee name, email, IsActive
  Identity role memberships    existing Domain.Role mirror
```

Create `ApplicationUser : IdentityUser<Guid>` in Infrastructure with an `EmployeeId` property. Identity `Id` and employee `ID` are different values in this design.

Configure a unique index on `EmployeeId` to enforce one Identity account per employee. A property or a unique index alone does not verify that the referenced employee exists: enforce the mapping during provisioning, and consider an explicit cross-table foreign-key migration later.

When creating accounts and roles, explicitly assign `Guid.NewGuid()` to their `Id`. Do not rely on the string-key Identity examples when choosing GUID keys. [Identity model customization](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/customize-identity-model?view=aspnetcore-10.0)

### Choose the role authority

For these exercises, **Identity role membership is the authorization source of truth**. Restrict accounts to exactly one of the project's four roles to match the current single-role employee model.

The existing Domain `User.Role` becomes a mirror maintained by a controlled provisioning/role-change workflow. Do not let it become a second independent permission source. Use `UserManager` for Identity membership updates and synchronize the profile in the same workflow.

Changing `Domain.User.Role` by itself will not update Identity roles or an existing login session. The current method is misspelled `ChageRole`; fix and test its naming yourself before adding a role-change command.

**Checkpoint:** Explain why comparing `Order.WaiterId` directly with the Identity account's `Id` would be wrong in this design.

## 5. Milestone 1: add Identity persistence

Build `RestaurantIdentityDbContext` in Infrastructure, inheriting:

```csharp
IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
```

Import `Microsoft.AspNetCore.Identity` and `Microsoft.AspNetCore.Identity.EntityFrameworkCore`. Give the context a constructor accepting its own `DbContextOptions`.

### Your tasks

1. Keep the existing `RestaurantPosDbContext` for business data.
2. Register the Identity context against the same PostgreSQL database using `UseNpgsql`.
3. In its `OnModelCreating`, call `base.OnModelCreating(builder)` before custom configuration.
4. Place Identity tables in schema `auth`, leaving existing business tables unchanged.
5. Configure the Identity context's migration-history table as `__EFMigrationsHistory` in schema `auth` using the Npgsql options callback's `MigrationsHistoryTable` setting.
6. Add the account-to-employee unique index described above.

A separate context avoids colliding with the existing business `DbSet<User> Users` and keeps your current initial migration separate. Identity's model includes its own users, roles, and membership tables. This is a project design choice; a single combined context is another option, but requires careful table/DbSet mapping. [Identity EF model](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/customize-identity-model?view=aspnetcore-10.0)

Use separate migration histories when both contexts target one database. [EF migration-history configuration](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/history-table)

### Generate and inspect the new migration

Run from the repository root after registering your context:

```powershell
dotnet ef migrations add InitialIdentity --context RestaurantIdentityDbContext --project backend/src/RestaurantPOS.Infrastructure/RestaurantPOS.Infrastructure.csproj --startup-project backend/src/RestaurantPOS.Api/RestaurantPOS.Api.csproj --output-dir Identity/Migrations
```

Read the generated migration. Expect Identity tables and indexes under `auth`; it should not drop or rename business tables.

Then apply it:

```powershell
dotnet ef database update --context RestaurantIdentityDbContext --project backend/src/RestaurantPOS.Infrastructure/RestaurantPOS.Infrastructure.csproj --startup-project backend/src/RestaurantPOS.Api/RestaurantPOS.Api.csproj
```

After adding a second context, specify the business context explicitly for its migrations:

```powershell
dotnet ef database update --context RestaurantPosDbContext --project backend/src/RestaurantPOS.Infrastructure/RestaurantPOS.Infrastructure.csproj --startup-project backend/src/RestaurantPOS.Api/RestaurantPOS.Api.csproj
```

`--context` selects which context the EF CLI operates on. [EF CLI reference](https://learn.microsoft.com/en-us/ef/core/cli/dotnet)

**Checkpoint:** PostgreSQL has separate business and Identity tables, and each context can apply its own migrations.

## 6. Milestone 2: register Identity and the request pipeline

Register the Identity context, then use this registration shape in your composition root or an Infrastructure extension invoked there:

```csharp
services.AddIdentity<ApplicationUser, IdentityRole<Guid>>()
    .AddEntityFrameworkStores<RestaurantIdentityDbContext>()
    .AddDefaultTokenProviders();
```

`AddIdentity` supplies managers, role-aware principal creation, and Identity cookie authentication. You do not need an extra `AddJwtBearer` registration for this cookie track. [ASP.NET Core 10 Identity registration source](https://github.com/dotnet/aspnetcore/blob/v10.0.0/src/Identity/Core/src/IdentityServiceCollectionExtensions.cs)

Choose and configure password, unique-email, lockout, and session-lifetime options deliberately. For the first exercise, use local seeded accounts; account confirmation and recovery are later milestones.

Call `ConfigureApplicationCookie` after Identity registration. Your target settings are an HttpOnly cookie, secure-only transport, `SameSite=Lax` for the same-origin arrangement, and a bounded lifetime. Preserve the existing cookie event callbacks; replacing the whole `Events` object can remove Identity's security-stamp validation. [Identity configuration](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/identity-configuration?view=aspnetcore-10.0)

Add authorization services. Middleware order should include:

```csharp
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
```

If you explicitly call `UseRouting`, put it before authentication. If you later use CORS middleware, place it before authentication and authorization.

Use `[ApiController]` for the JSON controllers. In ASP.NET Core 10, known API endpoints return `401`/`403` for cookie challenges/forbids instead of redirecting to HTML login pages. Verify that your endpoints actually have this behavior. [ASP.NET Core 10 cookie/API behavior](https://learn.microsoft.com/en-us/aspnet/core/breaking-changes/10/cookie-authentication-api-endpoints?view=aspnetcore-10.0)

**Checkpoint:** Explain the difference between registering authentication services and running authentication middleware.

## 7. Milestone 3: provision employee accounts and seed roles

Implement an idempotent role initializer using `RoleManager<IdentityRole<Guid>>`. Create missing `Waiter`, `Kitchen`, `Cashier`, and `Manager` roles. Check every `IdentityResult` for success.

For local learning, provision a Manager and one account for each operational role through an explicit Development-only bootstrap routine. Read passwords from local secrets, not source files. For example, you can set a bootstrap secret yourself:

```powershell
dotnet user-secrets init --project backend/src/RestaurantPOS.Api/RestaurantPOS.Api.csproj
dotnet user-secrets set "Bootstrap:ManagerPassword" "<your-local-password>" --project backend/src/RestaurantPOS.Api/RestaurantPOS.Api.csproj
```

Run the seed only when deliberately enabled in Development. A second run must not duplicate employees or accounts, reset existing passwords, or promote an existing account silently.

### Provisioning algorithm to implement

1. Validate the chosen role against the four-role allowlist.
2. Create or deliberately select the matching Domain employee profile.
3. Create the Identity account with its own GUID and the profile's `EmployeeId` link.
4. Set username/email according to your chosen login convention; using email as username is sufficient for the first exercise.
5. Call `UserManager.CreateAsync(account, password)` so Identity handles the credential.
6. Assign the selected Identity role through `UserManager.AddToRoleAsync`.
7. Keep the Domain role mirror consistent and report success only after the whole provisioning workflow succeeds.

With two contexts, separate saves are not automatically one transaction. Do not claim account creation succeeded when role assignment failed. Before exposing a Manager provisioning endpoint, finish the transaction/failure-handling exercise in section 14.

There is no public employee self-registration endpoint in this design. A future `CreateEmployeeAccountCommand` is Manager-authorized. Never let an anonymous registrant choose `Manager` or supply someone else's employee ID.

**Checkpoint:** Every seeded account links to exactly one employee and has exactly one allowed role. No password hash appears in API responses or application logs.

## 8. Milestone 4: build the HTTPS and CSRF path

Browsers automatically send authentication cookies, so cookie-based write endpoints need antiforgery protection. Protect login and logout too. `SameSite` and CORS are not replacements for token validation. [ASP.NET antiforgery guidance](https://learn.microsoft.com/en-us/aspnet/core/security/anti-request-forgery?view=aspnetcore-10.0)

### Angular-to-API transport

Start the API's existing HTTPS profile:

```powershell
dotnet dev-certs https --trust
dotnet run --project backend/src/RestaurantPOS.Api/RestaurantPOS.Api.csproj --launch-profile https
```

It exposes `https://localhost:7067`. Create an Angular development proxy file pointing `/api/**` at that URL:

```json
{
  "/api/**": {
    "target": "https://localhost:7067",
    "secure": false
  }
}
```

`secure: false` is only for the local proxy's connection to the development certificate. Use valid certificates and verification outside local development.

From `frontend/restaurant-pos`, run:

```powershell
npm start -- --ssl --proxy-config proxy.conf.json
```

Open the frontend with HTTPS. `ng serve --ssl` may use its own self-signed certificate; trusting the .NET certificate does not automatically trust Angular's certificate. Configure/trust the local frontend certificate as needed.

Use relative URLs such as `/api/auth/login` in Angular. The proxy makes requests same-origin from the browser's perspective. The current Angular application will also need `provideHttpClient()` registration when you implement HTTP services. [Angular development proxy](https://angular.dev/tools/cli/serve#proxying-to-a-backend-server)

### Token exchange you will implement

Configure `AddAntiforgery` with header name `X-XSRF-TOKEN`. Create `GET /api/auth/csrf`, available before login, that:

1. Calls `IAntiforgery.GetAndStoreTokens(HttpContext)`.
2. Takes its **`RequestToken`**, not `CookieToken`.
3. Writes that request token to a readable `XSRF-TOKEN` cookie with path `/`, secure transport, and the same-origin SameSite setting.
4. Leaves ASP.NET's separate antiforgery cookie HttpOnly.
5. Prevents caching of the token response.

The authentication cookie remains HttpOnly. Only the request-token cookie is deliberately readable by Angular.

Angular's default XSRF support reads `XSRF-TOKEN` and sends `X-XSRF-TOKEN` on same-origin/relative mutating requests. Calling the API with an absolute cross-origin URL can bypass that header behavior. [Angular XSRF guidance](https://angular.dev/best-practices/security#httpclient-xsrf-csrf-security)

### Validate on the server

For the controller exercises, use `AddControllersWithViews` and register a global `AutoValidateAntiforgeryTokenAttribute` filter. It provides the MVC services needed by that filter even though your endpoints return JSON and never render a view.

Your current `AddControllers` registration plus `AddAntiforgery` alone does not register all the services needed by that stock filter. If you keep `AddControllers`, build an explicit API filter around `IAntiforgery.ValidateRequestAsync` instead. Pick one approach and test it. [Version 10 MVC filter-service registration](https://github.com/dotnet/aspnetcore/blob/v10.0.0/src/Mvc/Mvc.ViewFeatures/src/DependencyInjection/MvcViewFeaturesMvcCoreBuilderExtensions.cs)

Validate every unsafe controller method, including POST login/logout and POST/PUT/PATCH/DELETE business operations. Do not bypass antiforgery on login simply because it is anonymous. If you later add Minimal API JSON endpoints, apply explicit validation there too; the MVC filter does not cover them.

Fetch a new CSRF request token after login and after logout because the token is tied to the current identity. Keep login/logout and token-refresh UI requests sequenced.

**Checkpoint:** A write with the valid cookie pair and header succeeds; the same write with a missing or mismatched antiforgery token fails before changing data.

## 9. Milestone 5: build login, logout, and current employee

Plan these endpoints; they are not implemented yet:

| Endpoint | Purpose | Access |
| --- | --- | --- |
| `GET /api/auth/csrf` | Bootstrap/renew the CSRF token | Anonymous allowed |
| `POST /api/auth/login` | Verify credentials and issue the Identity cookie | Anonymous allowed; CSRF validated |
| `POST /api/auth/logout` | Clear the current browser's auth cookie | Authenticated; CSRF validated |
| `GET /api/auth/me` | Return the caller's linked employee and current roles | Active employee |

### Login exercise

Accept email and password. Load the Identity account through `UserManager`, resolve its linked employee, and check the employee is active. Use `SignInManager.PasswordSignInAsync` with `lockoutOnFailure: true`, allowing a session only when its result indicates success.

Use a generic login-failure response. Handle locked-out, not-allowed, and two-factor-required outcomes deliberately; a two-factor-required result is not completed authentication. Do not issue a cookie just because a user record exists or a password check passed. [Identity sign-in and lockout flow](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/identity?view=aspnetcore-10.0#log-in)

Identity does not automatically enforce your Domain `IsActive` flag. An inactive or missing employee link must not receive a POS login session. Also require exactly one of the four allowed Identity roles before login succeeds, so an incompletely provisioned account cannot sign in.

For the first exercise, the login controller may call an Infrastructure account service that wraps the managers. If you put login orchestration behind a MediatR command, keep `SignInManager` and HTTP cookie effects behind that service boundary; do not inject it into Domain or Application directly.

Exclude passwords from any generic request logging behavior. Never return the Identity account entity from an endpoint.

### Define your own current-user contracts

| Application contract | What it exposes |
| --- | --- |
| `ICurrentUser` | `IsAuthenticated` and nullable `IdentityUserId` |
| `ICurrentEmployeeAccess` | An async current-access lookup accepting a cancellation token |
| `EmployeeAccessSnapshot` | Identity ID, employee ID, current allowed roles, and active state |

The API implements `ICurrentUser` using the principal established by authentication. Identity's user-ID claim defaults to `ClaimTypes.NameIdentifier`; parse it safely and fail closed if missing or invalid. [Identity claim configuration](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/identity-configuration?view=aspnetcore-10.0#claims-identity)

An Infrastructure implementation of the access lookup loads the Identity account by that trusted ID, resolves its `EmployeeId` mapping, reads the employee's current `IsActive`, and obtains current roles from Identity. Reject the lookup if the account has no role, multiple roles, or any role outside the four-role allowlist. Cache this snapshot only within the request for these exercises.

Do not populate it from JSON, query parameters, an unverified header, or a decoded-but-unvalidated token. A GUID identifying the actor comes from this lookup, not the command's input.

Return a small `me` DTO: employee ID, display name, and role names. The frontend can use it for navigation; the server still checks every protected operation.

### Session freshness exercise

For learning, configure `SecurityStampValidatorOptions.ValidationInterval` to `TimeSpan.Zero`. Keep Identity's existing validator enabled. After successful role changes or employee deactivation, call `UserManager.UpdateSecurityStampAsync` and check its result. An old cookie then fails stamp validation on a subsequent request. [Version 10 stamp-validation behavior](https://github.com/dotnet/aspnetcore/blob/v10.0.0/src/Identity/Core/src/SecurityStampValidator.cs)

This adds database work to authenticated requests. Later choose a documented production validation interval/revocation strategy. Also read current employee activity in authorization, so a disabled employee is blocked even if a workflow failed to update the stamp.

Do not treat browser logout as revoking every session everywhere: `SignOutAsync` clears that browser's cookie. Use a server-side invalidation operation when you need to end all existing sessions.

**Checkpoint:** Log in, call `me`, log out, and verify protected endpoints reject the next request. Then disable an employee and prove their old session cannot perform a protected operation.

## 10. Milestone 6: implement RBAC policies

Use this as a **proposed permission matrix**. These permissions do not exist in the current API; you will implement them:

| Operation | Waiter | Kitchen | Cashier | Manager | Additional condition |
| --- | --- | --- | --- | --- | --- |
| Read menu | Yes | Yes | Yes | Yes | Active employee |
| Create/edit menu | No | No | No | Yes | Active employee |
| Open a table session | Yes | No | No | No | Table usable; trusted waiter ID |
| Submit an order | Yes | No | No | No | Own draft order, own open session |
| Read kitchen queue | No | Yes | No | No | Queue limited to intended workflow states |
| Prepare/mark order ready | No | Yes | No | No | Permitted order state |
| Verify payment | No | No | Yes | No | Pending payment and eligible session |
| Provision/change employee roles | No | No | No | Yes | Controlled account-management workflow |

Choose any Manager overrides explicitly later and give them tests and audit records. Do not assume a Manager can impersonate a waiter or verify a payment automatically.

### Your policy exercises

1. Define role-name constants matching the existing enum names.
2. Write an `ActiveEmployeeRequirement` and a scoped handler that uses your current-access service.
3. Register named policies such as `ManageMenu`, `WorkKitchen`, `VerifyPayments`, and `ManageEmployees`.
4. Each named policy requires authentication, an active linked employee, and its allowed roles.
5. Apply the matching named policies to API endpoints.

The role portion uses the built-in syntax:

```csharp
policy.RequireRole("Manager");
```

Multiple names in one `RequireRole` call mean any listed role may pass. Multiple requirements in the policy must all pass. A role claim alone cannot express order ownership. [ASP.NET role authorization](https://learn.microsoft.com/en-us/aspnet/core/security/authorization/roles?view=aspnetcore-10.0)

Register authorization handlers that access scoped services as scoped, not singleton. During request authentication, the stamp strategy from milestone 5 refreshes/rejects stale cookie principals; do not silently drop that strategy when adding role policies.

Configure both the default and fallback policies to require an authenticated active employee. Mark only intentional public endpoints anonymous. Named policies do not automatically inherit the fallback requirements, so include the common requirements in each named policy. [Policy selection rules](https://learn.microsoft.com/en-us/aspnet/core/security/authorization/policies?view=aspnetcore-10.0)

**Checkpoint:** A Waiter gets `403` from a Manager-only operation; an anonymous caller gets `401`; a Manager passes when active. Tampering with the frontend's displayed role changes none of those results.

## 11. Milestone 7: implement ABAC for submitting an order

Use attributes that already exist in the repository:

- [Order](../backend/src/RestaurantPOS.Domain/Entities/Order.cs): `WaiterId`, `TableSessionId`, `Status`.
- [TableSession](../backend/src/RestaurantPOS.Domain/Entities/TableSession.cs): `WaiterId`, `Status`.
- Current caller: mapped employee ID, current Identity roles, active state.

Adopt this explicit policy for the exercise:

```text
Permit SubmitOrder when ALL are true:
  caller is authenticated and has an active linked employee
  current Identity roles include Waiter
  order.WaiterId == caller.EmployeeId
  session.WaiterId == caller.EmployeeId
  order.Status == Draft
  session.Status == Open
```

The session must be the one referenced by the order, loaded using `order.TableSessionId`. Do not accept a client-supplied session as evidence.

### Build the resource policy yourself

1. Define an Application-owned `SubmitOrderAccessResource` containing the current actor snapshot and the server-loaded ownership/state attributes above.
2. Define `SubmitOrderRequirement : IAuthorizationRequirement` in API.
3. Implement `AuthorizationHandler<SubmitOrderRequirement, SubmitOrderAccessResource>`.
4. Grant success only when every policy condition holds; confirm the actor snapshot corresponds to the authenticated principal.
5. Register a `SubmitOwnOrder` policy containing the requirement and authentication requirement.
6. Call `IAuthorizationService.AuthorizeAsync` with the current principal, trusted resource, and policy name.

Small call-shape reference:

```csharp
var decision = await authorizationService.AuthorizeAsync(
    principal, resource, "SubmitOwnOrder");
```

An endpoint attribute cannot inspect an order that has not been loaded yet. Resource authorization must happen after loading and before mutation. [ASP.NET resource authorization](https://learn.microsoft.com/en-us/aspnet/core/security/authorization/resource-based?view=aspnetcore-10.0)

Do not apply `[Authorize(Policy = "SubmitOwnOrder")]` directly to the endpoint: middleware has an HTTP resource, not your loaded `SubmitOrderAccessResource`. Use the default active-employee policy or a coarse Waiter policy there, then invoke `SubmitOwnOrder` explicitly after loading the order and session.

If you load current attributes inside an authorization handler, use request-scoped services. If you load them in the CQRS handler first, pass the trusted snapshot through your authorization interface. Never deserialize the authorization resource from the request body.

### Keep permission checks separate from lifecycle protection

This exercise treats a closed session or non-draft order as a failed ABAC permission condition. A denied authenticated caller gets `403` and the mutation never runs. Domain methods still defend lifecycle rules if called elsewhere or if state changes after authorization.

The current domain operation is `Order.Submit()`, with state `Submitted`, not the older docs' `Confirm()` / `Confirmed`. `MarkRead()` is the current method spelling for the Ready transition; fix and test naming yourself before kitchen commands.

**Checkpoint:** Changing an order ID to another waiter's ID does not grant access, even when the caller has the Waiter role.

## 12. Milestone 8: enforce permissions inside CQRS

HTTP endpoint policies provide the entry check. Your Application use cases must also reject unauthorized execution through `ISender.Send`; otherwise a new controller or another caller can bypass a controller-only check.

### Add an Application authorization boundary

Define `IOrderAccessService` with an async `EnsureCanSubmitAsync(SubmitOrderAccessResource resource, CancellationToken cancellationToken)` operation. Implement it in API as an adapter around ASP.NET resource authorization and the authenticated principal.

The adapter throws your Application `UnauthenticatedException` or `ForbiddenAccessException` on denial. Application never needs `ClaimsPrincipal` or `IAuthorizationService` directly. A future non-HTTP host must supply its own trusted actor/authorization adapter; do not substitute an allow-all implementation.

Extend the API exception mapping from the Application workbook: map `UnauthenticatedException` to `401` and `ForbiddenAccessException` to `403`, using the configured scheme's challenge/forbid behavior or equivalent API error responses. Otherwise an exception from a denied MediatR command can become an incorrect `500`. Keep this HTTP translation outside Application.

Build equivalent role permission checks for other secured commands and queries using current-access data. For example, `CreateMenuCategoryCommand` must check the current active employee's authoritative Identity membership includes Manager before staging anything.

### Submit-order handler algorithm

1. Get the authenticated, mapped employee snapshot; fail if absent or inactive.
2. Load the order by requested `OrderId` through a repository.
3. Load its linked session and construct the access resource from stored data.
4. Await `EnsureCanSubmitAsync`.
5. Only then call the domain `Submit()` method.
6. Save the change and return the result.

`SubmitOrderCommand` accepts the order ID. It does not accept `WaiterId`, a role, an `IsActive` flag, or an ownership assertion.

For missing orders, use your not-found convention. For denied resources, use `403` in this workbook; if you later conceal resource existence with `404`, apply that choice consistently.

### Pipeline and query exercises

After explicit checks work, you may add a MediatR authorization behavior for coarse request permissions. Mark requests that need an authenticated employee or a named permission. Keep resource-specific checks in the handler after loading the resource.

Run authentication/coarse permission checks before executing protected use cases; arrange validation and authorization order deliberately. Login is an anonymous entry point, so an authentication-required behavior cannot apply to every request without exemptions.

Resource authorization still cannot be skipped by a generic `RequireRole` marker. Reuse permission rules rather than writing contradictory controller and handler policies.

For reads, implement `GetMyOrdersQuery` with a server-derived employee ID and a database ownership filter. Authorize detail queries before returning DTOs. Hiding UI links or filtering after exposing the full list is not a data-access boundary.

**Checkpoint:** Send the command directly through MediatR with a fake unauthorized actor. No state mutation or save occurs.

## 13. Milestone 9: extend ABAC to cashier actions

The current [Payment](../backend/src/RestaurantPOS.Domain/Entities/Payment.cs) has `TableSessionId`, `Status`, `VerifiedByCashierId`, and `Verify(Guid cashierId)`.

For an initial exercise, permit verification only when the caller is an active Cashier, the payment is `Pending`, and its linked session is `PaymentPending`. Treat this as your proposed use-case policy and test each condition.

Load the payment and session on the server, authorize, and pass the **mapped employee ID** to `Verify`. Do not let the request supply `VerifiedByCashierId`.

The docs describe recording and issuing actors that are not implemented yet: there is no `Payment.RecordedByWaiterId` and no `Receipt.IssuedByCashierId`. A rule forbidding someone from verifying their own recorded payment needs a model change first.

Likewise, restaurant/tenant boundaries, shift schedules, and kitchen-station assignments are future ABAC extensions; the current model has none of those attributes. Add persisted attributes, trusted lookup rules, and tests before promising those restrictions.

Keep payment verification, receipt creation, and session closure consistent. Permission checks do not by themselves prevent concurrent duplicate receipts or double verification.

## 14. Milestone 10: handle administration and consistency

Before exposing account administration, implement a controlled workflow for provisioning, changing roles, and deactivating employees:

- Authorize the Manager using current server-side access data.
- Validate role names and maintain the one-role rule.
- Update Identity membership and the employee role mirror together.
- After role change/deactivation, invalidate existing sessions using the security stamp.
- Check every Identity result and handle failed persistence explicitly.
- Record actor ID, target employee ID, operation, and outcome without credential/session secrets.
- Prevent deleting/deactivating/demoting the last active Manager, including concurrent requests.

Do not accept a successful account save as proof that role assignment also succeeded. If provisioning spans two contexts in this design, implement a transaction using a shared relational connection and enlist both contexts in the same transaction, or design an explicit recoverable provisioning state that cannot log in while incomplete. Merely using the same connection string does not share a transaction. [EF cross-context transactions](https://learn.microsoft.com/en-us/ef/core/saving/transactions#cross-context-transaction)

For the transaction exercise, inspect how Identity's EF store performs saves, then test a failure after account creation but before role assignment. The final state must not leave a usable partially provisioned account. Test role-change rollback too.

Check access before modifying state, but account for changes between check and save. Use appropriate database constraints/concurrency control for ownership/state changes and administration invariants. Antiforgery and authorization solve different problems from concurrent state updates.

## 15. Write tests that prove the boundary

### Application and policy unit tests

Use fake current-user/current-access services and fake resource authorization for Application tests. Test actual ASP.NET authorization handlers separately with controlled principals and resources; an always-allow fake does not prove the policy works.

| Scenario | Expected |
| --- | --- |
| Anonymous caller sends a protected command | Unauthenticated; no staging/save |
| Waiter sends menu-edit command | Forbidden; no staging/save |
| Active Manager sends menu-edit command | Allowed |
| Active Waiter submits own Draft order in own Open session | Allowed |
| Waiter submits another waiter's order | Forbidden |
| Order owner matches but session owner differs | Forbidden |
| Order not Draft or session not Open | Forbidden by the exercise's ABAC policy |
| Employee missing, inactive, or account link invalid | Denied |
| Account has zero/multiple roles or an unknown role | Denied, including login |
| Claimed actor snapshot differs from principal | Denied |
| Active Cashier verifies eligible payment | Allowed; cashier ID is server-derived |
| Cashier tries an ineligible payment/session | Denied before verification |

Test every individual failed condition, not only the successful route.

### Identity/API integration tests

Your Tests project currently references Application and Domain only. For API-host tests, add an API project reference and the matching ASP.NET test-host package yourself:

```powershell
dotnet add backend/src/RestaurantPOS.Tests/RestaurantPOS.Tests.csproj reference backend/src/RestaurantPOS.Api/RestaurantPOS.Api.csproj
dotnet add backend/src/RestaurantPOS.Tests/RestaurantPOS.Tests.csproj package Microsoft.AspNetCore.Mvc.Testing --version 10.0.12
```

Follow the test-host setup, including making the top-level API entry point accessible to `WebApplicationFactory`. Use a separate test database; tests must not clear your development database. [ASP.NET integration testing](https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-10.0)

Use the real cookie handler and Identity stores for these checks:

- Correct credentials and CSRF pair issue a session; incorrect credentials do not.
- Repeated failures enforce the configured lockout; login errors remain generic.
- Protected API requests without a valid session return `401`, not an HTML redirect.
- A valid session with insufficient permission gets `403`.
- Missing/mismatched CSRF tokens reject login, logout, and business mutations.
- Logout removes the current browser session.
- Role changes and deactivation prevent the old cookie from completing a protected action.
- Login/logout followed by a fresh CSRF bootstrap permits the next legitimate mutation.
- An account-provisioning failure does not leave a usable partial account.
- A seeded account maps to the same Domain employee used in `WaiterId`/cashier audit fields.

Run from the repository root:

```powershell
dotnet build backend/src/RestaurantPOS.sln
dotnet test backend/src/RestaurantPOS.Tests/RestaurantPOS.Tests.csproj
```

## 16. Browser checks and later production work

Angular guards and hidden buttons improve navigation but do not authorize data access. Use `me` to render the UI, then let the API enforce permissions. Verify denials by calling the API directly as well as through the UI.

For this same-origin proxy flow, the browser sends the cookie automatically. If you later split frontend/API origins, explicitly design allowed origins, credentialed requests, cookie SameSite settings, and CSRF validation together; wildcard credentialed CORS is not a valid solution.

Before production use, complete session/key persistence and rotation, login rate limiting, account recovery/confirmation, appropriate MFA for privileged users, audit handling, and a clear revocation strategy. Persist/protect shared ASP.NET Data Protection keys when sessions must survive deployment or work across API instances. [Data Protection configuration](https://learn.microsoft.com/en-us/aspnet/core/security/data-protection/configuration/overview?view=aspnetcore-10.0)

These are later implementation milestones, not features supplied merely by adding an Identity package.

## 17. Troubleshooting

| Symptom | Check |
| --- | --- |
| EF reports multiple DbContexts | Include the intended `--context` |
| Identity migration wants to drop business tables | Check context separation, schema mapping, and migration history |
| RoleManager cannot be resolved | Use role-capable Identity registration with the chosen role type |
| `401` becomes a login-page redirect | Confirm API endpoint metadata and cookie challenge behavior |
| Cookie never accompanies the request | Check HTTPS, cookie attributes, proxy path, and browser host |
| Angular sends no XSRF header | Register HttpClient; fetch token; use relative same-origin mutating URLs |
| CSRF fails immediately after login/logout | Fetch a new request token for the changed identity |
| MVC antiforgery filter service cannot be resolved | Use the MVC registration supporting that filter or explicit API validation |
| Roles changed but old access still works | Check stamp updates, validation interval, and current-access lookups |
| An inactive employee still signs in | Domain IsActive needs an explicit login/access check |
| Waiter ownership check always fails | Resolve Identity Id -> EmployeeId; compare to Domain IDs |
| Direct MediatR calls bypass protection | Move/reuse permission enforcement in the use case |
| Identity API token fails JWT validation | Those built-in bearer tokens are not JWTs |

## 18. Your first work session

Start with the account model and persistence. Your deliverables are:

- [ ] Explain Identity account ID versus Domain employee ID.
- [ ] Create `ApplicationUser` and `RestaurantIdentityDbContext` in Infrastructure.
- [ ] Register PostgreSQL stores and inspect an Identity migration.
- [ ] Seed the four roles and deliberately provision a local Manager.
- [ ] Build the HTTPS/CSRF path before exposing login or write operations.
- [ ] Implement login, logout, and `me` with current employee checks.
- [ ] Protect one menu command and prove Waiter denial and Manager success.

Then move on to the owned-order ABAC exercise. Be able to answer: **Which part proves who I am? Which part grants menu permissions? Which part checks whose order this is? Which identifier is stored in WaiterId?**
