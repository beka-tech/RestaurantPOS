# Implementation guide (lean MVP)

How to write every slice in the [build plan](BUILD_PLAN.md): one pattern, used everywhere. Read the matching section before you start a WBS row. The code below is a skeleton to adapt; check constructor parameter orders against the real entities.

**Contents:** [1 Backend shape](#1-backend-shape) · [2 Folder layout](#2-folder-layout) · [3 Building blocks](#3-building-blocks) · [4 A slice end to end](#4-a-slice-end-to-end) · [5 Errors](#5-errors) · [6 Auth with JWT](#6-auth-with-jwt) · [7 Angular wiring](#7-angular-wiring) · [8 Tests](#8-tests) · [9 Run it](#9-run-it) · [10 Package it](#10-package-it)

## 1. Backend shape

The project references stay exactly as they are:

```text
Api            -> Application, Infrastructure
Infrastructure -> Application, Domain
Application    -> Domain
Domain         -> (nothing)
```

| Project | Owns in the MVP | Never contains |
| --- | --- | --- |
| Domain | Entities, status methods, `DomainException` guards | EF, HTTP, the current user |
| Application | One service per feature, request/response records, `IAppDbContext`, `ICurrentUser`, small exceptions | SQL, `HttpContext`, status codes |
| Infrastructure | `RestaurantPosDbContext`, entity configurations, migrations, seeding, password hashing, JWT creation | Business rules |
| Api | Controllers, `Program.cs` wiring, exception → HTTP mapping, reading the current user from the token | Business rules |

MediatR and FluentValidation are referenced by Application but not used in v1, so don't register them. Their workbook is the [v2 track](APPLICATION_LAYER_GUIDE.md).

## 2. Folder layout

Create each file only when its WBS row needs it. The feature folders follow the plan: one folder per business feature, with clean layering inside.

```text
backend/src/
  RestaurantPOS.Domain/
    Common/DomainException.cs ................................ 1.2
  RestaurantPOS.Application/
    Common/IAppDbContext.cs .................................. 1.4
    Common/Exceptions.cs   (NotFound, Forbidden, Unauthorized)  1.4
    Common/ICurrentUser.cs ................................... 2.1
    Common/Security.cs     (IPasswordService, ITokenService) . 2.1
    Features/Auth/        AuthService.cs, AuthDtos.cs ........ 2.1
    Features/Employees/   EmployeeService.cs, EmployeeDtos.cs  2.3
    Features/Menu/        MenuService.cs, MenuDtos.cs ........ 3.1
    Features/Tables/      TableService.cs, TableDtos.cs ...... 3.3
    Features/Orders/      OrderService.cs, OrderDtos.cs ...... 4.1
    Features/Kitchen/     KitchenService.cs, KitchenDtos.cs .. 4.3
    Features/Payments/    PaymentService.cs, PaymentDtos.cs .. 5.1–5.2
    DependencyInjection.cs (AddApplication) .................. 1.4
  RestaurantPOS.Infrastructure/
    Persistence/RestaurantPosDbContext.cs  (add OnModelCreating) 1.3
    Persistence/Configurations/<Entity>Configuration.cs ...... 1.3
    Persistence/DbSeeder.cs .................................. 2.1, 6.3
    Security/PasswordService.cs, JwtTokenService.cs, JwtOptions.cs 2.1
    DependencyInjection.cs (AddInfrastructure) ............... 1.4
  RestaurantPOS.Api/
    Controllers/<Feature>Controller.cs
    ErrorHandling/ApiExceptionHandler.cs ..................... 1.4
    Security/CurrentUser.cs .................................. 2.1
    Program.cs
  RestaurantPOS.Tests/
    Domain/<Entity>Tests.cs .................................. 1.2, 4.5, 5.4
```

## 3. Building blocks

Write these once (WBS 1.2 and 1.4); every slice reuses them.

```csharp
// Domain/Common/DomainException.cs: a business rule was broken (→ 409)
namespace RestaurantPOS.Domain.Common;
public sealed class DomainException(string message) : Exception(message);
```

```csharp
// Application/Common/Exceptions.cs
public sealed class NotFoundException(string what, Guid id) : Exception($"{what} {id} was not found."); // → 404
public sealed class ForbiddenException(string message) : Exception(message);                             // → 403
public sealed class UnauthorizedException() : Exception("Email or password is incorrect.");               // → 401
```

```csharp
// Application/Common/IAppDbContext.cs
// Add the Microsoft.EntityFrameworkCore package to Application (same version as Infrastructure).
// Use the DbSet names that already exist in RestaurantPosDbContext.
public interface IAppDbContext
{
    DbSet<User> Users { get; }
    DbSet<RestaurantTable> RestaurantTables { get; }
    DbSet<TableSession> TableSessions { get; }
    DbSet<MenuCategory> MenuCategories { get; }
    DbSet<MenuItem> MenuItems { get; }
    DbSet<Order> Orders { get; }
    DbSet<OrderItem> OrderItems { get; }
    DbSet<Payment> Payments { get; }
    DbSet<Receipt> Receipts { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
```

`RestaurantPosDbContext` implements it by adding `: IAppDbContext` to its declaration; the `DbSet` properties are already there.

```csharp
// Application/Common/ICurrentUser.cs: who is calling, taken from the token
public interface ICurrentUser
{
    Guid Id { get; }
    UserRole Role { get; }
}
```

## 4. A slice end to end

The example is WBS 3.3, "open a visit". Every other slice has the same shape.

**Request and response records** (validation attributes go on the constructor parameters):

```csharp
// Application/Features/Tables/TableDtos.cs
public sealed record OpenVisitRequest([Range(1, 50)] int GuestCount);
public sealed record OpenVisitResponse(Guid VisitId);
```

**Service:** load, call domain methods, save once.

```csharp
// Application/Features/Tables/TableService.cs
public sealed class TableService(IAppDbContext db, ICurrentUser me)
{
    public async Task<OpenVisitResponse> OpenVisitAsync(Guid tableId, OpenVisitRequest req, CancellationToken ct)
    {
        var table = await db.RestaurantTables.FirstOrDefaultAsync(t => t.ID == tableId, ct)
            ?? throw new NotFoundException("Table", tableId);

        table.Occupy();                                             // DomainException if not Available
        var visit = new TableSession(table.ID, me.Id, req.GuestCount); // match the real constructor
        db.TableSessions.Add(visit);

        await db.SaveChangesAsync(ct);                              // one save = one transaction
        return new OpenVisitResponse(visit.ID);
    }
}
```

If two waiters open the same table at the same moment, both see Available. The partial unique index then rejects the second insert, and the error handler returns 409. You don't need any extra locking code.

**Controller:** thin. It maps HTTP to the service and nothing more.

```csharp
// Api/Controllers/TablesController.cs
[ApiController]
[Route("api/tables")]
public sealed class TablesController(TableService tables) : ControllerBase
{
    [HttpPost("{id:guid}/sessions")]
    [Authorize(Roles = "Waiter")]
    public async Task<ActionResult<OpenVisitResponse>> Open(Guid id, OpenVisitRequest req, CancellationToken ct)
        => Ok(await tables.OpenVisitAsync(id, req, ct));
}
```

**Registration:** add one line per new service.

```csharp
// Application/DependencyInjection.cs
public static IServiceCollection AddApplication(this IServiceCollection services) => services
    .AddScoped<AuthService>()
    .AddScoped<EmployeeService>()
    .AddScoped<MenuService>()
    .AddScoped<TableService>()
    .AddScoped<OrderService>()
    .AddScoped<KitchenService>()
    .AddScoped<PaymentService>();
```

```csharp
// Infrastructure/DependencyInjection.cs: move the AddDbContext call here from Program.cs
public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
{
    services.AddDbContext<RestaurantPosDbContext>(o =>
        o.UseNpgsql(config.GetConnectionString("Default")));   // use the name in appsettings.Development.json
    services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<RestaurantPosDbContext>());
    services.Configure<JwtOptions>(config.GetSection("Jwt"));
    services.AddSingleton<IPasswordService, PasswordService>();
    services.AddSingleton<ITokenService, JwtTokenService>();
    return services;
}
```

**The ownership check (decision D-08)** is one line in each waiter action:

```csharp
if (visit.WaiterId != me.Id) throw new ForbiddenException("This visit belongs to another waiter.");
```

## 5. Errors

One handler turns exceptions into readable JSON, so services just throw.

| Thrown | HTTP | Example |
| --- | --- | --- |
| Invalid request body (DataAnnotations; `[ApiController]` handles it automatically) | 400 | `GuestCount = 0` |
| `UnauthorizedException`, or a missing or expired token | 401 | Wrong password |
| `ForbiddenException`, or the wrong role for `[Authorize(Roles = …)]` | 403 | A waiter ordering on another waiter's visit |
| `NotFoundException` | 404 | Unknown table ID |
| `DomainException` | 409 | Verifying a payment that is already Verified |
| Unique-index violation (PostgreSQL `23505`) | 409 | Opening the same table twice |
| Anything else | 500 | Logged; the client sees a generic message |

```csharp
// Api/ErrorHandling/ApiExceptionHandler.cs
public sealed class ApiExceptionHandler(ILogger<ApiExceptionHandler> log) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext http, Exception ex, CancellationToken ct)
    {
        var (status, title) = ex switch
        {
            UnauthorizedException => (401, ex.Message),
            ForbiddenException    => (403, ex.Message),
            NotFoundException     => (404, ex.Message),
            DomainException       => (409, ex.Message),
            DbUpdateException { InnerException: PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } }
                => (409, "This was already done or changed by someone else. Refresh and try again."),
            _ => (500, "Something went wrong.")
        };
        if (status == 500) log.LogError(ex, "Unhandled error");

        http.Response.StatusCode = status;
        await http.Response.WriteAsJsonAsync(new ProblemDetails { Status = status, Title = title }, ct);
        return true;
    }
}
```

## 6. Auth with JWT

This is WBS 2.1–2.2. It takes about one day instead of the two weeks the Identity-cookie design needs. The trade-off is stated in decision D-09, and the [v2 workbook](AUTHENTICATION_AUTHORIZATION_GUIDE.md) is the upgrade path.

**Configuration.** Keep the key out of git:

```json
// appsettings.json
"Jwt": { "Issuer": "restaurant-pos", "Audience": "restaurant-pos" }
```

```powershell
dotnet user-secrets init --project backend/src/RestaurantPOS.Api
dotnet user-secrets set "Jwt:Key" "<at least 32 random characters>" --project backend/src/RestaurantPOS.Api
dotnet user-secrets set "Seed:ManagerPassword" "<your local password>" --project backend/src/RestaurantPOS.Api
```

In Docker, set them as environment variables: `Jwt__Key` and `Seed__ManagerPassword`.

**Password hashing** reuses ASP.NET Identity's hasher. Infrastructure already gets it through its Identity package; you don't need any Identity tables.

```csharp
// Application/Common/Security.cs
public interface IPasswordService { string Hash(User user, string password); bool Verify(User user, string password); }
public interface ITokenService    { string CreateToken(User user); }

// Infrastructure/Security/PasswordService.cs
public sealed class PasswordService : IPasswordService
{
    private readonly PasswordHasher<User> _hasher = new();
    public string Hash(User user, string password) => _hasher.HashPassword(user, password);
    public bool Verify(User user, string password) =>
        user.PasswordHash is not null &&
        _hasher.VerifyHashedPassword(user, user.PasswordHash, password) != PasswordVerificationResult.Failed;
}
```

**Token creation.** Add the `Microsoft.IdentityModel.JsonWebTokens` package to Infrastructure, at the version `dotnet list package --include-transitive` shows for the Api project.

```csharp
// Infrastructure/Security/JwtTokenService.cs
public sealed class JwtOptions { public string Issuer { get; set; } = ""; public string Audience { get; set; } = ""; public string Key { get; set; } = ""; }

public sealed class JwtTokenService(IOptions<JwtOptions> options) : ITokenService
{
    public string CreateToken(User user)
    {
        var jwt = options.Value;
        return new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = jwt.Issuer,
            Audience = jwt.Audience,
            Expires = DateTime.UtcNow.AddHours(8),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)), SecurityAlgorithms.HmacSha256),
            Claims = new Dictionary<string, object>
            {
                ["sub"] = user.ID.ToString(),
                ["name"] = user.FullName,
                ["role"] = user.Role.ToString()      // "Manager", "Waiter", …
            }
        });
    }
}
```

**Login service.** Give one generic error for every failure, so the response never reveals which part was wrong.

```csharp
// Application/Features/Auth/AuthService.cs
public sealed class AuthService(IAppDbContext db, IPasswordService passwords, ITokenService tokens)
{
    public async Task<LoginResponse> LoginAsync(LoginRequest req, CancellationToken ct)
    {
        var email = req.Email.Trim().ToLowerInvariant();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);
        if (user is null || !user.IsActive || !passwords.Verify(user, req.Password))
            throw new UnauthorizedException();

        return new LoginResponse(tokens.CreateToken(user), new MeDto(user.ID, user.FullName, user.Role.ToString()));
    }
}
```

**Program.cs:** the complete MVP wiring.

```csharp
var builder = WebApplication.CreateBuilder(args);
var jwt = builder.Configuration.GetSection("Jwt").Get<JwtOptions>()!;

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.MapInboundClaims = false;                     // keep "sub" and "role" exactly as issued
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
            NameClaimType = "name",
            RoleClaimType = "role",                     // makes [Authorize(Roles = "Manager")] work
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });
builder.Services.AddAuthorization();

var app = builder.Build();
app.UseExceptionHandler();
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();                         // Scalar.AspNetCore package: browsable API UI
    await app.Services.MigrateAndSeedAsync(builder.Configuration);
}
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
```

**Current user** (Api) reads the two claims you issued:

```csharp
// Api/Security/CurrentUser.cs
public sealed class CurrentUser(IHttpContextAccessor http) : ICurrentUser
{
    private string Claim(string type) =>
        http.HttpContext?.User.FindFirst(type)?.Value ?? throw new UnauthorizedException();
    public Guid Id => Guid.Parse(Claim("sub"));
    public UserRole Role => Enum.Parse<UserRole>(Claim("role"));
}
```

**Seed the first Manager** so you can sign in on a fresh database:

```csharp
// Infrastructure/Persistence/DbSeeder.cs
public static class DbSeeder
{
    public static async Task MigrateAndSeedAsync(this IServiceProvider services, IConfiguration config)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RestaurantPosDbContext>();
        var passwords = scope.ServiceProvider.GetRequiredService<IPasswordService>();

        await db.Database.MigrateAsync();
        if (await db.Users.AnyAsync()) return;

        var manager = new User("Main", "Manager", "manager@pos.local", UserRole.Manager); // match the real constructor
        manager.SetPasswordHash(passwords.Hash(manager, config["Seed:ManagerPassword"]!));
        db.Users.Add(manager);
        await db.SaveChangesAsync();
        // WBS 6.3: add demo categories, items and tables here too
    }
}
```

**Last active Manager (WBS 2.3).** Check it before any demote or deactivate:

```csharp
if (target.Role == UserRole.Manager && target.IsActive &&
    await db.Users.CountAsync(u => u.Role == UserRole.Manager && u.IsActive, ct) == 1)
    throw new DomainException("You can't deactivate or demote the last active Manager.");
```

**Permission matrix:**

| Action | Waiter | Kitchen | Cashier | Manager |
| --- | --- | --- | --- | --- |
| Read sellable menu (`GET menu`) | ✓ | | | ✓ |
| Edit menu, tables, employees | | | | ✓ |
| Open visit, send order, mark Served, request bill, record payment | ✓ (own visits) | | | |
| Kitchen queue, start, ready | | ✓ | | |
| Pending payments, verify, reject | | | ✓ | |
| Read receipt | | | ✓ | ✓ |

## 7. Angular wiring

**Dev proxy.** It makes the API same-origin, so there's no CORS setup and no certificate hassle. Create `frontend/restaurant-pos/proxy.conf.json`:

```json
{ "/api": { "target": "http://localhost:5257", "secure": false } }
```

Then start Angular with `ng serve --proxy-config proxy.conf.json`, and call relative URLs such as `/api/tables`.

**Bootstrap** (`app.config.ts`):

```ts
export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes),
    provideHttpClient(withInterceptors([authInterceptor])),
  ],
};
```

**API clients.** Reuse the scaffolded files in `core/services/` (`auth.ts`, `menu.ts`, `order.ts`, `payment.ts`, `table.ts`) and add `employee.ts`. Rename each class to `<Feature>Api` (`AuthStore` for auth) so it doesn't clash with model type names like `Order`. The snippets use `@Injectable({ providedIn: 'root' })`; the scaffold's `@Service()` decorator works as well.

```ts
// core/services/auth.ts: token and current user, kept in a signal
export interface Me { id: string; name: string; role: 'Waiter' | 'Kitchen' | 'Cashier' | 'Manager'; }

@Injectable({ providedIn: 'root' })
export class AuthStore {
  private http = inject(HttpClient);
  private router = inject(Router);
  readonly me = signal<Me | null>(JSON.parse(localStorage.getItem('pos.me') ?? 'null'));

  login(email: string, password: string) {
    return this.http.post<{ token: string; user: Me }>('/api/auth/login', { email, password }).pipe(
      tap(res => {
        localStorage.setItem('pos.token', res.token);
        localStorage.setItem('pos.me', JSON.stringify(res.user));
        this.me.set(res.user);
      }));
  }

  token() { return localStorage.getItem('pos.token'); }

  logout() {
    localStorage.removeItem('pos.token');
    localStorage.removeItem('pos.me');
    this.me.set(null);
    this.router.navigateByUrl('/login');
  }
}
```

```ts
// core/auth.interceptor.ts: attach the token, and send the user to /login on 401
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthStore);
  const token = auth.token();
  const authed = token ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } }) : req;
  return next(authed).pipe(
    catchError((err: HttpErrorResponse) => {
      if (err.status === 401 && !req.url.endsWith('/auth/login')) auth.logout();
      return throwError(() => err);
    }));
};
```

```ts
// core/role.guard.ts: navigation convenience only; the API still checks every request
export const roleGuard = (...roles: Me['role'][]): CanActivateFn => () => {
  const me = inject(AuthStore).me();
  const router = inject(Router);
  if (!me) return router.parseUrl('/login');
  return roles.includes(me.role) ? true : router.parseUrl('/');
};
```

**Routes** (`app.routes.ts`) follow the plan's route map:

```ts
export const routes: Routes = [
  { path: 'login', loadComponent: () => import('./features/auth/pages/login/login').then(m => m.Login) },
  {
    path: '',
    component: MainLayout,
    canActivate: [roleGuard('Waiter', 'Kitchen', 'Cashier', 'Manager')],
    children: [
      { path: 'waiter/tables', canActivate: [roleGuard('Waiter')],
        loadComponent: () => import('./features/tables/pages/table-list/table-list').then(m => m.TableList) },
      { path: 'waiter/sessions/:id', canActivate: [roleGuard('Waiter')],     // the visit page: picker, cart, orders, bill
        loadComponent: () => import('./features/orders/pages/create-order/create-order').then(m => m.CreateOrder) },
      { path: 'cashier', canActivate: [roleGuard('Cashier')],
        loadComponent: () => import('./features/payments/pages/payment/payment').then(m => m.Payment) },
      { path: 'manager/menu', canActivate: [roleGuard('Manager')],
        loadComponent: () => import('./features/menu/pages/menu-list/menu-list').then(m => m.MenuList) },
      // New pages, same shape: 'kitchen' (Kitchen), 'receipts/:id' (Cashier, Manager),
      // 'manager/tables' and 'manager/employees' (Manager)
    ],
  },
  { path: '**', redirectTo: '' },
];
```

The paths above use the scaffolded page folders. Check each class name in its `.ts` file.

**Polling instead of SignalR (WBS 4.4).** The kitchen board refreshes every 5 seconds:

```ts
readonly orders = toSignal(
  timer(0, 5000).pipe(switchMap(() => this.api.kitchenQueue())),
  { initialValue: [] as KitchenOrder[] });
```

After the cook clicks Start or Ready, call the API and refresh the list straight away.

## 8. Tests

Test only the rules that lose money or data. The Tests project already references Domain. Name tests `Method_Situation_Result`.

| Test | WBS |
| --- | --- |
| `OrderItem_ValidArgs_KeepsOrderId` · `OrderItem_EmptyOrderId_Throws` | 1.2 |
| `ChangeName_TrimmedName_IsAssigned` · `ChangeName_Blank_Throws` | 1.2 |
| `Submit_NoItems_Throws` · `Total_SumsUnitPriceTimesQuantity` | 1.2, 4.5 |
| `Order_FullPath_SubmittedToServed` · `MarkServed_FromSubmitted_Throws` | 4.5 |
| `Payment_TelebirrWithoutReference_Throws` · `Payment_CashWithoutReference_IsAllowed` | 1.2, 5.4 |
| `Verify_Twice_Throws` · `Reject_StoresReasonAndTime` | 5.4 |
| `Close_FromOpen_Throws` · `ReturnToBillRequested_FromPaymentPending_Works` | 1.2, 5.4 |

The database guards (one open visit per table, one receipt per payment) are proven by the demo-script steps "open Table 3 twice" and "click Verify twice" in v1. Automated PostgreSQL tests are v2.

## 9. Run it

From the repository root:

```powershell
docker compose up -d                                         # PostgreSQL on localhost:5434
dotnet tool install --global dotnet-ef                       # once
dotnet ef migrations add MvpIntegrity --project backend/src/RestaurantPOS.Infrastructure --startup-project backend/src/RestaurantPOS.Api
dotnet run --project backend/src/RestaurantPOS.Api --launch-profile http    # http://localhost:5257; migrates and seeds in Development
dotnet test backend/src/RestaurantPOS.sln
```

```powershell
cd frontend/restaurant-pos
npx ng serve --proxy-config proxy.conf.json                  # http://localhost:4200
```

For a clean slate during development, run `docker compose down -v`. This deletes the local database, so use it only while it holds nothing but test data.

## 10. Package it

This is WBS 6.4, and it is the first thing to cut if time is short.

1. `npx ng build` and copy `dist/restaurant-pos/browser/*` into the Api's `wwwroot/`.
2. In `Program.cs`, before `MapControllers`, add `app.UseDefaultFiles(); app.UseStaticFiles();`. After it, add `app.MapFallbackToFile("index.html");`. Angular and the API then share one origin.
3. Write a two-stage Dockerfile for the Api: an SDK image to publish, then an `aspnet` runtime image. Add an `api` service to `docker-compose.yml` with `ConnectionStrings__Default`, `Jwt__Key` and `Seed__ManagerPassword` as environment variables. Receipt numbers use the `Africa/Addis_Ababa` time zone, so if you pick an Alpine or chiseled base image, install `tzdata` in it.
4. Run migrations on startup in the container too: call `MigrateAndSeedAsync` outside the Development check, or behind a `RUN_MIGRATIONS=true` flag.
