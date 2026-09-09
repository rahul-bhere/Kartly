# Kartly.API  Backend (.NET 8 Web API, Clean Architecture)

REST API for the Kartly shopping app: JWT auth, role-based authorization,
products, cart, orders, and simulated payments  backed by SQL Server via
EF Core (Code First).

> **Note on this deliverable:** this backend was written directly as
> source code (no `dotnet` SDK / NuGet access was available in the
> environment that generated it), so it has **not** been compiled or run
> here. The code follows standard, well-tested ASP.NET Core / EF Core
> patterns throughout, but please run `dotnet restore` and `dotnet build`
> as your first step and treat any compiler errors as normal first-run
> setup, not a fundamentally broken design.

---

## Architecture: Clean Architecture, 4 projects

```
kartly-backend/
├── Kartly.sln
└── src/
    ├── Kartly.Domain/           # Entities, enums. Zero dependencies.
    ├── Kartly.Application/      # DTOs, interfaces, services (business logic),
    │                             # AutoMapper profile, FluentValidation validators.
    │                             # Depends only on Domain.
    ├── Kartly.Infrastructure/    # EF Core DbContext, repositories, JWT service,
    │                             # BCrypt password hasher. Implements
    │                             # Application's interfaces.
    └── Kartly.API/                # Controllers, Program.cs, appsettings,
                                    # middleware. The composition root 
                                    # the only project that references all others.
```

**Dependency direction (the whole point of Clean Architecture):**
`API → Infrastructure → Application → Domain`. Domain knows nothing about
any other layer. Application defines interfaces (`IProductRepository`,
`IJwtTokenService`, etc.) that Infrastructure implements  Application
never references Infrastructure directly. This is what lets you swap SQL
Server for PostgreSQL, or BCrypt for ASP.NET Identity, by changing
Infrastructure alone.

---

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- SQL Server: either full SQL Server, **SQL Server Express**, or
  **LocalDB** (installed automatically with Visual Studio). LocalDB is
  the easiest option for local-only development and is what the default
  connection string below assumes.
- (Optional) [Azure Data Studio](https://azure.microsoft.com/products/data-studio) or SSMS to inspect the database.

---

## Setup  Step by Step

### 1. Restore and build
```bash
cd kartly-backend
dotnet restore
dotnet build
```

### 2. Set the JWT signing secret (REQUIRED  the app will not start without it)

This project deliberately ships `Jwt:Key` as an **empty string** in the
committed `appsettings.json` so a real secret can never accidentally end
up in source control. You have two options:

**Option A  quick local testing (already done for you):**
`appsettings.Development.json` is gitignored and already contains a
placeholder key, so `dotnet run` will work immediately when
`ASPNETCORE_ENVIRONMENT=Development` (the default for `dotnet run`).
You should still replace the placeholder with your own random string.

**Option B  the proper way, recommended even for solo projects:**
```bash
cd src/Kartly.API
dotnet user-secrets init
dotnet user-secrets set "Jwt:Key" "paste-a-long-random-string-here-at-least-32-characters"
cd ../..
```
User Secrets are stored **outside the project folder entirely** (in your
OS user profile), so there is no file to accidentally commit at all.
The `cd ../..` at the end returns you to the `kartly-backend` root 
every command from here on in this README assumes you're there.

Generate a strong random key quickly:
```bash
# macOS/Linux
openssl rand -base64 48
# Windows PowerShell
[Convert]::ToBase64String((1..48 | ForEach-Object { Get-Random -Maximum 256 }))
```

### 3. Connect to SQL Server (first time setup + SSMS credentials)

**Which SQL Server are you using?** Pick the matching credentials below.

**A) SQL Server LocalDB** (bundled with Visual Studio  easiest, no
install needed if you have VS):
- Server name in SSMS / Azure Data Studio: `(localdb)\mssqllocaldb`
- Authentication: **Windows Authentication** (no username/password  just connect)
- This is what `appsettings.json` already assumes by default:
  ```
  Server=(localdb)\mssqllocaldb;Database=KartlyDb;Trusted_Connection=True;TrustServerCertificate=True
  ```
  **No changes needed** if you're using LocalDB  skip to Step 4.

**B) SQL Server Express / Developer / full SQL Server, server name "localhost"**
(this is the common case if you installed SQL Server directly, e.g. via
the installer from microsoft.com  not through Visual Studio):

- In SSMS, connect with:
  - Server name: `localhost` (or `localhost\SQLEXPRESS` if you installed
    a *named instance*  check the installer's summary screen or your
    Windows Services list for a service named `SQL Server (SQLEXPRESS)`
    vs plain `SQL Server (MSSQLSERVER)`)
  - Authentication: try **Windows Authentication** first (default,
    no password)  if that connects, use it.
  - If Windows Authentication fails, use **SQL Server Authentication**
    with username `sa` and the password you set when installing SQL
    Server (the installer asks you to set this during a "Mixed Mode"
    setup  if you don't remember setting one, Windows Authentication is
    almost certainly the right choice instead).

- Once connected in SSMS, update **`appsettings.Development.json`** (or
  via `dotnet user-secrets`, same as the JWT key) with a connection
  string matching what worked in SSMS:

  Windows Authentication, default instance:
  ```
  Server=localhost;Database=KartlyDb;Trusted_Connection=True;TrustServerCertificate=True
  ```
  Windows Authentication, named instance (e.g. SQLEXPRESS):
  ```
  Server=localhost\SQLEXPRESS;Database=KartlyDb;Trusted_Connection=True;TrustServerCertificate=True
  ```
  SQL Server Authentication (username/password):
  ```
  Server=localhost;Database=KartlyDb;User Id=sa;Password=YOUR_PASSWORD;TrustServerCertificate=True
  ```

  Set it via user-secrets (recommended, keeps it out of any committed file):
  ```bash
  cd src/Kartly.API
  dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost;Database=KartlyDb;Trusted_Connection=True;TrustServerCertificate=True"
  cd ../..
  ```

- **You do NOT need to manually create the `KartlyDb` database or any
  tables in SSMS.** Step 4 below (EF Core migrations) creates the
  database and every table automatically. Just confirm you can connect
  to the *server* in SSMS  the specific database doesn't need to exist yet.

- **Common connection problem:** if `localhost` refuses to connect at
  all (not an auth error, but "cannot connect"/timeout), open **SQL
  Server Configuration Manager** → SQL Server Network Configuration →
  Protocols for [your instance] → make sure **TCP/IP is Enabled**, then
  restart the SQL Server service. This trips up a lot of first-time
  installs.

### 4. Create the database with EF Core migrations (Code First)

**Run every command in this README from the `kartly-backend` root
folder** (the one containing `Kartly.sln`)  not from inside
`src/Kartly.API`. Mixing this up is the #1 cause of "Unable to retrieve
project metadata" errors.

```bash
# from kartly-backend/ (NOT from inside src/Kartly.API)
dotnet tool install --global dotnet-ef   # first time only on this machine
dotnet ef migrations add InitialCreate --project src/Kartly.Infrastructure --startup-project src/Kartly.API
dotnet ef database update --project src/Kartly.Infrastructure --startup-project src/Kartly.API
```
This reads the entity classes in `Kartly.Domain/Entities` and the Fluent
API configuration in `Kartly.Infrastructure/Persistence/ApplicationDbContext.cs`
and generates/applies the actual SQL Server schema  that's what "Code
First" means.

> **If this still fails with "Unable to retrieve project metadata":**
> that error usually means the solution doesn't *build* yet, not that
> the paths are wrong. Run `dotnet build` from `kartly-backend/` first
> and fix any compiler errors shown there  `dotnet ef` silently reports
> the generic metadata error instead of the real build error underneath it.

> The app also calls `Database.MigrateAsync()` automatically on startup
> (see `DbSeeder.SeedAsync`), so once migrations exist, just running the
> app applies them. You still need to *create* the first migration by hand
> with the command above.

### 5. Run the API
```bash
# from kartly-backend/
dotnet run --project src/Kartly.API
```
Swagger UI opens at: `https://localhost:5001/swagger` (or whatever port
the console output shows).

---

## AI Assistant (Admin Dashboard)

An admin-only chat tab that performs real product CRUD from natural
language, using Claude's tool-use API. Full write-up (setup, request
flow, interview talking points) in
[`../docs/AI_ASSISTANT.md`](../docs/AI_ASSISTANT.md). Quick setup:
```bash
cd src/Kartly.API
dotnet user-secrets set "Anthropic:ApiKey" "sk-ant-your-real-key-here"
```

## Connecting to the Frontend

The short version: CORS is already configured for
`http://localhost:5173` (see `Cors:AllowedOrigins` in `appsettings.json`)
and the port is fixed by `launchSettings.json`, so the frontend's
`VITE_ADMIN_API_URL` just needs to match. For the full walkthrough
(including how to move the *shopper* side onto this backend too, not
just the admin dashboard), see
[`../docs/INTEGRATION_GUIDE.md`](../docs/INTEGRATION_GUIDE.md).

## Architecture Diagrams & Theory

See [`../docs/PROJECT_STRUCTURE.md`](../docs/PROJECT_STRUCTURE.md) for a
file-by-file trace of what happens inside this backend when the admin
creates a product, plus the reasoning behind the generic repository +
Unit of Work pattern used throughout `Kartly.Infrastructure`.

## Logging in as Admin (seeded automatically)

On first run, `DbSeeder` creates one admin account if none exists:

```
username: admin
password: Admin@123
```

**Change this password (or delete and recreate the user) before using
this anywhere beyond your own machine.** There is no self-service
"change password" endpoint in this version  update it directly via the
database or add one as a next step.

Regular user accounts are created through `POST /api/auth/register`
(always assigned the `User` role  public registration can never create
an admin, by design in `AuthService`).

---

## Authentication Flow

1. `POST /api/auth/register` or `/api/auth/login` → returns
   `{ accessToken, refreshToken, accessTokenExpiresAt, user }`.
2. Send `Authorization: Bearer <accessToken>` on every subsequent request.
3. Access tokens expire after 30 minutes (`Jwt:AccessTokenExpiryMinutes`).
   When one expires, call `POST /api/auth/refresh` with the refresh token
   to get a new pair (refresh tokens rotate  the old one is revoked each
   time).
4. `POST /api/auth/logout` revokes a specific refresh token.

JWT claims included: `NameIdentifier` (user id), `Name` (username), `Role`
(`User` or `Admin`). Role-based endpoints use
`[Authorize(Roles = "Admin")]`.

---

## API Endpoints Overview

| Area | Method & Route | Auth |
|---|---|---|
| Auth | `POST /api/auth/register` | Public |
| Auth | `POST /api/auth/login` | Public |
| Auth | `POST /api/auth/refresh` | Public |
| Auth | `POST /api/auth/logout` | Any user |
| Products | `GET /api/products` `GET /api/products/{id}` | Public |
| Products | `POST /api/products` `PUT /api/products/{id}` `DELETE /api/products/{id}` | **Admin** |
| Cart | `GET/POST/PUT/DELETE /api/cart...` | Any user |
| Orders | `POST /api/orders/checkout` `GET /api/orders/mine` `GET /api/orders/{id}` | Any user |
| Orders | `GET /api/orders` `PUT /api/orders/{id}/status` | **Admin** |
| Payments | `POST /api/payments` `GET /api/payments/order/{orderId}` | Any user |
| Users | `GET/PUT /api/users/me` | Any user |
| Users | `GET /api/users` `PUT/DELETE /api/users/{id}` | **Admin** |

Full request/response shapes are documented live in Swagger
(`/swagger`)  every DTO is visible there with examples.

---

## Validation

Every write endpoint's DTO is validated automatically by a global action
filter (`Filters/ValidationFilter.cs`) that runs the matching
FluentValidation validator (`Application/Validators/*`) before the
controller method executes. A failed validation returns `400` with a
`{ errors: { field: [messages] } }` shape  no controller needs manual
`ModelState` checks.

---

## Security Summary

- **Passwords:** hashed with BCrypt (work factor 12), never stored or logged in plain text.
- **JWT:** short-lived (30 min) access tokens + rotating refresh tokens (7 days), signed with HMAC-SHA256 using a secret that is never committed to source control.
- **Role-based authorization:** `[Authorize(Roles = "Admin")]` on every admin-only endpoint; ownership checks in services (e.g. a user can only see their own orders) as a second layer beyond the role check.
- **Secrets:** `appsettings.Development.json` and `appsettings.Production.json` are gitignored (`.gitignore`); the committed `appsettings.json` only ever contains placeholders. Use `dotnet user-secrets` locally and environment variables / a secrets manager (Azure Key Vault, AWS Secrets Manager) in any real deployment.
- **CORS:** locked to explicit origins (`Cors:AllowedOrigins` in config)  not wide open.

---

## Known Simplifications (by design, documented so nothing looks accidental)

- **Payments are simulated.** `PaymentService.ProcessSimulatedCharge` never
  contacts a real payment gateway. Swapping in Stripe/Razorpay/PayPal
  later means replacing the inside of that one method  the DTO shapes,
  controller, and database schema are already gateway-agnostic.
- **Migrations must be generated by you** (`dotnet ef migrations add`)
  since this was authored without a live .NET/NuGet environment to run
  that command in. The `Migrations/` folder does not exist yet  creating
  it is step 4 above.
- **No email verification / password reset flow**  out of scope for a
  portfolio-stage app but a natural next feature.
