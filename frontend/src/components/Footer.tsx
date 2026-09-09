import { Link } from "react-router-dom";

export function Footer() {
  return (
    <footer className="mt-16 border-t border-[var(--color-line)] bg-[var(--color-surface)]">
      <div className="mx-auto flex max-w-6xl flex-col gap-6 px-6 py-10 sm:flex-row sm:items-start sm:justify-between">
        <div>
          <p className="text-lg font-700 text-[var(--color-ink)]" style={{ fontFamily: "var(--font-display)" }}>Kartly</p>
          <p className="mt-1 max-w-xs text-sm text-[var(--color-ink-soft)]">A full-stack shopping demo - React + .NET 8 + SQL Server, with an AI assistant that does real CRUD.</p>
        </div>
        <div className="flex gap-10 text-sm">
          <div className="flex flex-col gap-2">
            <span className="font-600 text-[var(--color-ink)]">Company</span>
            <Link to="/about" className="text-[var(--color-ink-soft)] hover:text-[var(--color-ink)]">About us</Link>
            <Link to="/contact" className="text-[var(--color-ink-soft)] hover:text-[var(--color-ink)]">Contact us</Link>
          </div>
          <div className="flex flex-col gap-2">
            <span className="font-600 text-[var(--color-ink)]">Shop</span>
            <Link to="/" className="text-[var(--color-ink-soft)] hover:text-[var(--color-ink)]">Catalog</Link>
            <Link to="/orders" className="text-[var(--color-ink-soft)] hover:text-[var(--color-ink)]">Your orders</Link>
          </div>
        </div>
      </div>
      <div className="border-t border-[var(--color-line)] px-6 py-4 text-center text-xs text-[var(--color-ink-soft)]">
        © {new Date().getFullYear()} Kartly. Portfolio project — not a real store.
      </div>
    </footer>
  );
}
