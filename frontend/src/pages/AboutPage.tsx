export function AboutPage() {
  return (
    <div className="mx-auto max-w-2xl px-6 py-14">
      <h1 className="text-3xl font-700 text-[var(--color-ink)]" style={{ fontFamily: "var(--font-display)" }}>About Kartly</h1>
      <p className="mt-4 leading-relaxed text-[var(--color-ink-soft)]">
        Kartly is a full-stack e-commerce project: a React + TypeScript frontend
        paired with a .NET 8 Web API backend (Clean Architecture, JWT auth, SQL Server via
        EF Core Code First). It's built to demonstrate a realistic set of features end to
        end - authentication, a merged product catalog, cart and checkout, order tracking,
        an admin dashboard, and an AI assistant that performs real database operations
        through natural language.
      </p>
      <p className="mt-4 leading-relaxed text-[var(--color-ink-soft)]">
        See <code>docs/ARCHITECTURE.md</code> in the project repository for the full design
        write-up: why each layer exists, which design patterns are used and why, and how
        every request flows from the browser down to the database.
      </p>
    </div>
  );
}
