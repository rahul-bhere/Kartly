import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { useAuth } from "../../context/AuthContext";
import { ProductsTab } from "../../components/admin/ProductsTab";
import { UsersTab } from "../../components/admin/UsersTab";
import { OrdersTab } from "../../components/admin/OrdersTab";

type Tab = "products" | "users" | "orders";

// Uses the SAME session as the rest of the app now (useAuth, not a
// separate admin login) — AdminRoute already guarantees isAdmin is true
// by the time this renders. The floating AI assistant (bottom-right,
// visible on every page) covers admin chat now too, so there's no
// separate "Assistant" tab here anymore.
export function AdminDashboardPage() {
  const [tab, setTab] = useState<Tab>("products");
  const { user, logout } = useAuth();
  const navigate = useNavigate();

  function handleLogout() {
    logout();
    navigate("/login");
  }

  return (
    <div className="mx-auto max-w-6xl px-6 py-10">
      <div className="flex items-center justify-between">
        <div>
          <span className="font-mono text-xs uppercase tracking-wider text-[var(--color-accent)]">Admin dashboard</span>
          <h1 className="text-3xl font-700 text-[var(--color-ink)]" style={{ fontFamily: "var(--font-display)" }}>Welcome, {user?.firstName}</h1>
        </div>
        <button onClick={handleLogout} className="rounded-full border border-[var(--color-line)] px-4 py-2 text-sm text-[var(--color-ink)] hover:border-[var(--color-danger)] hover:text-[var(--color-danger)]">Log out</button>
      </div>

      <div className="mt-6 flex gap-2 border-b border-[var(--color-line)]">
        {(["products", "users", "orders"] as Tab[]).map((t) => (
          <button key={t} onClick={() => setTab(t)} className={`border-b-2 px-4 py-2.5 text-sm font-600 capitalize transition ${tab === t ? "border-[var(--color-primary)] text-[var(--color-ink)]" : "border-transparent text-[var(--color-ink-soft)] hover:text-[var(--color-ink)]"}`}>
            {t}
          </button>
        ))}
      </div>

      <div className="mt-6">
        {tab === "products" && <ProductsTab />}
        {tab === "users" && <UsersTab />}
        {tab === "orders" && <OrdersTab />}
      </div>
    </div>
  );
}
