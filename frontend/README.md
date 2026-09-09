# Kartly  Frontend (React + TypeScript)

The shopper-facing storefront and the admin dashboard, in one React app.
See the root `README.md` (one level up) for the full-project overview,
architecture diagram, and step-by-step full-stack roadmap.

## Features

**Shopper (real backend for auth  product catalog still dummy):**
- Register / Login / Profile  real, persisted in your SQL Server database via Kartly.API
- Product catalog: search, category filter, pagination (still the dummy DummyJSON catalog)
- Product detail page
- Cart: add / update quantity / remove / clear
- Checkout with simulated payment (Credit Card / PayPal / Cash on Delivery)
- Order history + order confirmation page

**Admin (real backend  your Kartly.API + SQL Server):**
- Separate Admin Login (`/admin/login`), authenticated against your own backend
- Admin Dashboard (`/admin`) with four tabs:
  - **Products**  full CRUD, saved to your SQL Server database
  - **Users**  view all users, toggle active/role, delete
  - **Orders**  view orders placed through the real backend, update status
  - **AI Assistant**  natural-language CRUD ("create a product called...") via Claude's tool-use API  see [`../docs/AI_ASSISTANT.md`](../docs/AI_ASSISTANT.md)

**Everywhere:**
- Light/dark theme toggle (persisted, respects OS preference on first visit)
- Responsive layout, accessible focus states, reduced-motion support

## Run locally

```bash
npm install
npm run dev
```

**This requires `kartly-backend` running locally** (see
`../kartly-backend/README.md`)  login/register now hit it directly, not
a dummy API.

Demo login: `admin` / `Admin@123` (seeded automatically on the backend's
first run), or register your own account at `/register`.

## Environment variables (`.env`)

```
VITE_API_URL=https://dummyjson.com
VITE_ADMIN_API_URL=https://localhost:5001/api
```

## Connecting to the backend & project structure theory

- [`../docs/INTEGRATION_GUIDE.md`](../docs/INTEGRATION_GUIDE.md)  exact steps/line diffs to connect this frontend to `kartly-backend`, and how to go further than just the admin dashboard.
- [`../docs/PROJECT_STRUCTURE.md`](../docs/PROJECT_STRUCTURE.md)  why `src/` is organized this way, with a file-by-file trace of the login and add-to-cart flows.

## Deploying to GitHub Pages

1. Push this `frontend` folder's contents to a GitHub repo (or a `frontend/` subfolder  adjust `homepage` accordingly).
2. In `package.json`, set `"homepage"` to your real GitHub Pages URL, e.g.
   `"https://yourusername.github.io/kartly"`.
3. In `vite.config.ts`, `base` is already set to `'./'` (relative paths),
   which works for most GitHub Pages setups without further changes.
4. Deploy:
   ```bash
   npm run deploy
   ```
   This builds the app and pushes `dist/` to a `gh-pages` branch, which
   GitHub Pages serves automatically (enable Pages → source: `gh-pages`
   branch, in your repo's Settings if it isn't already).
5. **Important:** the Admin Dashboard will not work from the deployed
   GitHub Pages site unless your backend is also publicly reachable 
   per the project requirements, the backend is meant to run **locally
   only**. This is expected: deploy the frontend for the shopper
   experience/portfolio link; run the admin backend locally when you want
   to demo the admin features.

## Folder structure

See the root README for the full annotated tree  the short version:

```
src/
├── api/            # HTTP calls  client.ts (dummy product API), authClient.ts + adminClient.ts (your backend)
├── types/          # Shared TS interfaces, mirroring backend DTOs
├── context/        # AuthContext (shopper), AdminAuthContext, ThemeContext
├── store/          # Zustand: cartStore, ordersStore
├── routes/         # ProtectedRoute, AdminProtectedRoute
├── components/      # Shared UI + components/admin/ (ProductsTab, UsersTab, OrdersTab)
├── pages/           # Route-level pages + pages/admin/
└── utils/           # format.ts, validation.ts
```
