# Kartly  Full-Stack Shopping Application

Kartly is a full-stack e-commerce demo: a React + TypeScript storefront
with light/dark themes, cart, checkout, and order history, backed by two
data sources  a public dummy API for the shopping experience, and your
own **.NET 8 Web API + SQL Server** backend (Clean Architecture, JWT
auth, role-based authorization) for admin-managed products, users, and
orders.

```
Kartly/
├── README.md              ← you are here
├── frontend/               ← React + TypeScript (Vite)
└── kartly-backend/         ← .NET 8 Web API, Clean Architecture
```

---

## 1. What's in the box

| Area | Description |
|---|---|
| **Shopper experience** | Register/login, browse/search/filter products, cart, checkout with simulated payment, order history, editable profile |
| **Admin experience** | Separate admin login (real backend), dashboard to manage products (full CRUD → SQL Server), view/manage users, view/update order status |
| **AI Assistant** | Chat tab in the Admin Dashboard  natural language CRUD on products (and read-only lookups on users/orders) via Claude's tool-use API. See [`docs/AI_ASSISTANT.md`](./docs/AI_ASSISTANT.md) |
| **Frontend** | React 19, TypeScript, Vite, React Router, TanStack Query, Zustand, Axios, Tailwind CSS v4, light/dark theme |
| **Backend** | ASP.NET Core 8 Web API, Clean Architecture (Domain/Application/Infrastructure/API), EF Core Code First, SQL Server, JWT + refresh tokens, BCrypt, AutoMapper, FluentValidation, Swagger |

---

## 2. Login Credentials

### Shopper login  now your real backend + SQL Server
Login and registration for shoppers hit your own Kartly.API backend
(products still come from the dummy catalog  see §3 for exactly which
parts are real vs. dummy). Two ways to get in:

- **Register a new account** at `/register`  it's a real row in your
  `Users` table, and you're logged in immediately afterward.
- **Or use the seeded admin account** to try it right away without
  registering:
  ```
  username: admin
  password: Admin@123
  ```
  (This logs you into the regular shopper experience as an Admin-role
  user  it works fine for browsing/cart/checkout too, it's just also
  the account with dashboard access at `/admin/login`.)

### Admin dashboard login (`/admin/login`)
Seeded automatically the first time the backend runs, by `DbSeeder`:

```
username: admin
password: Admin@123
```
**Change this password before using the app anywhere beyond your own
machine.**

---

## 3. Architecture Overview

![Kartly system architecture](./docs/architecture-system.svg)

*Two independent paths today: the Shopper UI talks only to DummyJSON; the
Admin UI talks only to your own Kartly.API backend. See
[`docs/INTEGRATION_GUIDE.md`](./docs/INTEGRATION_GUIDE.md) for how (and
whether) to merge these paths.*

![Request flow through Kartly.API](./docs/architecture-request-flow.svg)

*What happens, layer by layer, when the Admin Dashboard creates a
product  full narrative walkthrough in
[`docs/PROJECT_STRUCTURE.md`](./docs/PROJECT_STRUCTURE.md).*

For the full theory  why the code is organized this way, and a
file-by-file trace of what happens on a login, an add-to-cart, and an
admin product creation  see **[`docs/PROJECT_STRUCTURE.md`](./docs/PROJECT_STRUCTURE.md)**.

For exact, line-level steps to connect (or further integrate) the two
projects, see **[`docs/INTEGRATION_GUIDE.md`](./docs/INTEGRATION_GUIDE.md)**.

### 3.1 System architecture (text form, for quick reference)

```
┌─────────────────────────────────────────────────────────────────────┐
│                         BROWSER (React SPA)                          │
│                                                                       │
│   Shopper pages                        Admin pages                   │
│   (Catalog, Cart, Checkout,            (Admin Login, Dashboard:      │
│    Orders, Profile, Login)              Products / Users / Orders)   │
│         │                                       │                    │
│         ▼                                       ▼                    │
│   src/api/client.ts                    src/api/adminClient.ts        │
│   (axios + dummy-API token)            (axios + YOUR JWT token)      │
└─────────┬───────────────────────────────────────┬────────────────────┘
          │ HTTPS                                  │ HTTPS
          ▼                                         ▼
┌───────────────────────┐               ┌────────────────────────────────┐
│   DummyJSON (public)   │               │   Kartly.API  (localhost only)  │
│   products, auth        │               │   ASP.NET Core 8 Web API        │
│   (read-only, no        │               │   ┌────────────────────────┐   │
│   persistence)          │               │   │ Controllers (API layer) │   │
└───────────────────────┘               │   ├────────────────────────┤   │
                                          │   │ Application (services,  │   │
     Cart & Orders                        │   │ DTOs, validation)       │   │
     (client-side,                        │   ├────────────────────────┤   │
      localStorage via                    │   │ Infrastructure (EF Core,│   │
      Zustand)                            │   │ JWT, BCrypt repos)      │   │
                                          │   ├────────────────────────┤   │
                                          │   │ Domain (entities, rules)│   │
                                          │   └───────────┬────────────┘   │
                                          └───────────────┼────────────────┘
                                                           ▼
                                                 ┌─────────────────────┐
                                                 │   SQL Server (local)  │
                                                 │   KartlyDb             │
                                                 └─────────────────────┘
```

**Why two data sources?** Per the project's requirements: the shopping
catalog starts on a dummy API so the frontend can be fully built and
demoed without any backend setup, while admin-created products are real,
persisted data flowing through your own backend  a deliberate,
documented split (see each layer's code comments), not an inconsistency.

### 3.2 Backend: Clean Architecture layers

```
┌──────────────────────────────────────────────────────────┐
│  Kartly.API            (Presentation / composition root)   │
│  Controllers, Program.cs, Swagger, middleware, appsettings  │
└───────────────────────────┬──────────────────────────────┘
                             │ depends on
┌───────────────────────────▼──────────────────────────────┐
│  Kartly.Infrastructure  (implements Application's           │
│  interfaces)                                                │
│  EF Core DbContext, Repositories, JwtTokenService,           │
│  BCryptPasswordHasher, DbSeeder                              │
└───────────────────────────┬──────────────────────────────┘
                             │ depends on
┌───────────────────────────▼──────────────────────────────┐
│  Kartly.Application     (business logic  framework-free)   │
│  Services (Auth/Product/Cart/Order/Payment/User),             │
│  DTOs, Interfaces, AutoMapper profile, FluentValidation        │
└───────────────────────────┬──────────────────────────────┘
                             │ depends on
┌───────────────────────────▼──────────────────────────────┐
│  Kartly.Domain          (entities, enums  zero dependencies)│
│  User, Product, Cart, CartItem, Order, OrderItem, Payment,     │
│  RefreshToken                                                  │
└──────────────────────────────────────────────────────────┘
```
Dependencies only point **downward**. Domain knows nothing about EF Core
or ASP.NET; Application defines interfaces that Infrastructure
implements. This is what lets you swap SQL Server for PostgreSQL, or
BCrypt for ASP.NET Identity, by changing Infrastructure alone  nothing
above it needs to change.

### 3.3 Authentication flow

```
 User/Admin           Frontend                    Backend
    │                     │                            │
    │  enter credentials  │                            │
    ├────────────────────▶│                            │
    │                     │  POST /api/auth/login       │
    │                     ├───────────────────────────▶│
    │                     │                            │ verify (BCrypt)
    │                     │                            │ issue JWT (claims:
    │                     │                            │  UserId, Role) +
    │                     │                            │  refresh token
    │                     │◀───────────────────────────┤
    │                     │  store accessToken +         │
    │                     │  refreshToken (localStorage) │
    │                     │                            │
    │  browse protected   │  Authorization: Bearer <JWT> │
    │  page                ├───────────────────────────▶│
    │                     │                            │ [Authorize] checks
    │                     │                            │ signature + claims
    │                     │◀───────────────────────────┤
    │                     │  200 OK + data (or 401/403)  │
    │                     │                            │
    │  (30 min later)     │  POST /api/auth/refresh      │
    │  access token        ├───────────────────────────▶│
    │  expires             │                            │ rotate refresh token,
    │                     │◀───────────────────────────┤ issue new JWT
```

### 3.4 Order/checkout flow (current, simulated)

```
Cart (Zustand, localStorage)
   │  "Checkout"
   ▼
CheckoutPage  shipping address + payment method form
   │  "Pay"
   ▼
Simulated payment (~900ms delay, always "succeeds" for
Card/PayPal; "Pending" for Cash on Delivery)
   │
   ▼
ordersStore.placeOrder()  snapshots items, saves Order
to localStorage, clears cart
   │
   ▼
OrderDetailPage  confirmation, shown at /orders/:id
```
The **real backend already implements the equivalent flow** for real
(`OrdersController.Checkout` → `OrderService.CreateFromCartAsync` →
`PaymentsController.Pay` → `PaymentService.SimulatePaymentAsync`)  see
the roadmap below for wiring the frontend to call these instead.

---

## 4. Tech Stack

**Frontend:** React 19, TypeScript, Vite, React Router (HashRouter  see
why in `frontend/README.md`), TanStack Query, Zustand, Axios, Tailwind
CSS v4.

**Backend:** ASP.NET Core 8 Web API, Entity Framework Core 8 (SQL
Server), AutoMapper, FluentValidation, `System.IdentityModel.Tokens.Jwt`,
BCrypt.Net-Next, Swashbuckle (Swagger).

---

## 5. Security

- **Passwords:** BCrypt-hashed (work factor 12)  never stored or logged in plain text.
- **JWT:** 30-minute access tokens + 7-day rotating refresh tokens, HMAC-SHA256 signed. Claims: `UserId`, `Username`, `Role`.
- **Role-based authorization:** `[Authorize(Roles = "Admin")]` on every admin-only endpoint (product writes, user management, order status updates), plus ownership checks in services (e.g. a shopper can only view their own orders).
- **Validation:** every write DTO validated automatically via FluentValidation (global filter  see backend README).
- **Secrets never committed:** `appsettings.Development.json` (real local secrets) and `appsettings.Production.json` are gitignored; the committed `appsettings.json` only has placeholders. `dotnet user-secrets` is the recommended approach  see backend README §"Set the JWT signing secret".
- **CORS:** locked to explicit configured origins, not wide open.

---

## 6. Hosting

- **Frontend:** deployed to **GitHub Pages** (`npm run deploy` inside `frontend/`  see `frontend/README.md` for full steps). Uses `HashRouter` so client-side routes survive refreshes on GitHub Pages' static hosting.
- **Backend:** runs **locally only**, per project requirements (`dotnet run` against LocalDB/SQL Server on your machine). The deployed frontend's shopper experience works standalone (dummy API); the Admin Dashboard only works while your backend is running locally and `VITE_ADMIN_API_URL` points to it.

---

## 7. Step-by-Step: Running the Whole Thing Locally

1. **Backend first:**
   ```bash
   cd kartly-backend
   dotnet restore
   cd src/Kartly.API
   dotnet user-secrets init
   dotnet user-secrets set "Jwt:Key" "$(openssl rand -base64 48)"
   cd ../..
   dotnet ef migrations add InitialCreate --project src/Kartly.Infrastructure --startup-project src/Kartly.API
   dotnet ef database update --project src/Kartly.Infrastructure --startup-project src/Kartly.API
   dotnet run --project src/Kartly.API
   ```
   Confirm Swagger loads at `https://localhost:5001/swagger`.

2. **Frontend:**
   ```bash
   cd frontend
   npm install
   npm run dev
   ```
   Confirm `VITE_ADMIN_API_URL` in `.env` matches the port from step 1.

3. **Try both experiences:**
   - Shopper: register a new account (or log in with `admin` / `Admin@123`) at `/register` or `/login`  this now hits your real backend, browse the (still dummy) catalog, add to cart, checkout, view orders, edit your profile (also real  saved to SQL Server).
   - Admin: go to `/admin/login`, log in with `admin` / `Admin@123`, create a product, watch it appear in the Products tab (pulled live from your SQL Server database via Swagger too, if you want to double check).

---

## 8. Roadmap: Making It Fully Real End-to-End

The app is intentionally staged so each of these is a small, isolated
change rather than a rewrite. **Shopper auth (login/register/profile) is
already done**  items below are what's left:

1. **Move shopper checkout onto the real backend.** Replace
   `store/cartStore.ts` and `store/ordersStore.ts`'s local logic with
   calls to `CartController`/`OrdersController`/`PaymentsController`
   (already fully implemented in `kartly-backend`) via a new
   `src/api/cartApi.ts` / `orderApi.ts`. The shopper already carries a
   real JWT now (see `api/authClient.ts`)  it just isn't sent to these
   endpoints yet.
2. ~~Unify authentication.~~ **Done**  shopper login/register/profile
   now call the real backend via `api/authClient.ts` /
   `api/authApi.ts`, storing the same real JWT the Admin Dashboard uses
   (separate `localStorage` keys, so the two sessions still don't
   collide  see `docs/PROJECT_STRUCTURE.md` for why that's intentional
   even now).
3. **Replace the dummy product catalog.** Point `productApi.ts` at
   `GET /api/products` instead of DummyJSON once there's enough
   admin-created inventory  the `Product` type in `types/index.ts`
   already mirrors `ProductDto` closely.
4. **Real payments.** Swap the body of
   `PaymentService.ProcessSimulatedCharge` (backend) for a real gateway
   SDK call (Stripe/Razorpay/PayPal), keeping the same method signature 
   nothing in the controllers, DTOs, or frontend needs to change.
5. **Deploy the backend too**, if/when you're ready to go beyond local-only
   (Azure App Service, Railway, Render + a hosted SQL Server/Postgres),
   and update `VITE_ADMIN_API_URL` (or by then, just `VITE_API_URL`) to
   the live URL.
6. **Nice-to-haves:** password reset/email verification, product images
   upload instead of URLs, pagination on the admin product list, order
   emails, unit/integration tests.

---

## 9. Known Limitations (by design  documented so nothing looks accidental)

- **Products are still the dummy catalog**  shopper login/register/profile are real (SQL Server), but `productApi.ts` still calls DummyJSON. See roadmap step 3.
- **Shopper cart/orders are client-side only** (localStorage), not tied to the server-side account  even though the shopper now has a real JWT, checkout doesn't send it anywhere yet. See roadmap step 1.
- **Admin "Orders" tab will be empty at first**  it reads real orders from your backend, but the shopper checkout flow doesn't send orders there yet (roadmap step 1). Orders created directly via Swagger's `POST /api/orders/checkout` will show up correctly.
- **Payments are simulated everywhere** (frontend and backend)  no real money or card data is ever processed.
- **Backend migrations aren't pre-generated**  you run `dotnet ef migrations add InitialCreate` yourself (backend README §4) since this project was authored without a live .NET/NuGet environment to generate them in.

---

## 10. Folder-by-folder documentation

- [`frontend/README.md`](./frontend/README.md)  frontend setup, structure, GitHub Pages deploy steps
- [`kartly-backend/README.md`](./kartly-backend/README.md)  backend setup, migrations, JWT secret setup, full endpoint list
- [`docs/PROJECT_STRUCTURE.md`](./docs/PROJECT_STRUCTURE.md)  theory: why the project is organized this way, plus a file-by-file trace of key request flows
- [`docs/INTEGRATION_GUIDE.md`](./docs/INTEGRATION_GUIDE.md)  exact steps and line-level diffs to connect frontend ↔ backend, at two levels (admin-only, or full shopper integration)
- [`docs/AI_ASSISTANT.md`](./docs/AI_ASSISTANT.md)  the natural-language CRUD assistant: setup, request flow, and interview talking points
