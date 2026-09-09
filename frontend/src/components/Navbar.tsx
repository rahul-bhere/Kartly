import { Link, useNavigate } from "react-router-dom";
import { useAuth } from "../context/AuthContext";
import { useCartStore } from "../store/cartStore";
import { ThemeToggle } from "./ThemeToggle";

// NAV VISIBILITY RULE (one unified session now):
//   - Logged out entirely -> show BOTH "Log in" and "Admin" links.
//   - Logged in as a shopper (Role=User) -> show shopper nav, NO admin link.
//   - Logged in as Admin -> show shopper nav (they can shop too) PLUS the
//     Admin Dashboard link, but the plain "Log in" link is gone since
//     they're already authenticated.
// There's no such thing as "logged in as admin AND able to see the user
// login link" anymore, because it's the same session either way.
export function Navbar() {
  const { user, isAuthenticated, isAdmin, logout } = useAuth();
  const totalItems = useCartStore((s) => s.totalItems());
  const navigate = useNavigate();

  function handleLogout() {
    logout();
    navigate("/login");
  }

  return (
    <header className="sticky top-0 z-40 border-b border-[var(--color-line)] bg-[var(--color-paper)]/90 backdrop-blur">
      <div className="mx-auto flex max-w-6xl items-center justify-between px-6 py-4">
        <Link to="/" className="text-xl font-700 tracking-tight text-[var(--color-ink)]" style={{ fontFamily: "var(--font-display)" }}>Kartly</Link>

        <nav className="flex flex-wrap items-center gap-4 text-sm font-medium text-[var(--color-ink-soft)]">
          <Link to="/" className="transition hover:text-[var(--color-ink)]">Catalog</Link>
          <Link to="/about" className="hidden transition hover:text-[var(--color-ink)] sm:inline">About</Link>

          {isAuthenticated && (
            <>
              <Link to="/cart" className="relative flex items-center gap-1.5 transition hover:text-[var(--color-ink)]">
                Cart
                {totalItems > 0 && <span className="flex h-5 min-w-5 items-center justify-center rounded-full bg-[var(--color-primary)] px-1 font-mono text-[11px] font-600 text-white">{totalItems}</span>}
              </Link>
              <Link to="/orders" className="transition hover:text-[var(--color-ink)]">Orders</Link>
              <Link to="/profile" className="transition hover:text-[var(--color-ink)]">Profile</Link>
            </>
          )}

          {isAdmin && (
            <Link to="/admin" className="rounded-full bg-[var(--color-accent)] px-3 py-1 text-xs font-600 text-[#1c1c1e] transition hover:opacity-90">
              Admin Dashboard
            </Link>
          )}

          <ThemeToggle />

          {isAuthenticated ? (
            <div className="flex items-center gap-3 border-l border-[var(--color-line)] pl-4">
              <span className="hidden text-[var(--color-ink)] sm:inline">Hi, {user?.firstName}</span>
              <button onClick={handleLogout} className="rounded-full border border-[var(--color-line)] px-3 py-1.5 text-[var(--color-ink)] transition hover:border-[var(--color-danger)] hover:text-[var(--color-danger)]">Log out</button>
            </div>
          ) : (
            <div className="flex items-center gap-3 border-l border-[var(--color-line)] pl-4">
              <Link to="/login" className="transition hover:text-[var(--color-ink)]">Log in</Link>
              <Link to="/register" className="rounded-full bg-[var(--color-ink)] px-4 py-1.5 text-[var(--color-paper)] transition hover:bg-[var(--color-primary)]">Sign up</Link>
            </div>
          )}
        </nav>
      </div>
    </header>
  );
}
