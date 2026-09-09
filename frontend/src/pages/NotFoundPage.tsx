import { Link } from "react-router-dom";

export function NotFoundPage() {
  return (
    <div className="mx-auto flex min-h-[60vh] max-w-md flex-col items-center justify-center gap-3 px-6 text-center">
      <span
        className="font-mono text-sm text-[var(--color-ink-soft)]"
      >
        404
      </span>
      <h1
        className="text-2xl font-700 text-[var(--color-ink)]"
        style={{ fontFamily: "var(--font-display)" }}
      >
        Page not found
      </h1>
      <p className="text-sm text-[var(--color-ink-soft)]">
        The page you're looking for doesn't exist.
      </p>
      <Link to="/" className="text-sm font-600 text-[var(--color-primary)]">
        Back to catalog
      </Link>
    </div>
  );
}
