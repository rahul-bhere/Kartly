import { useState, type FormEvent } from "react";
import { Link, useNavigate } from "react-router-dom";
import { useAuth } from "../context/AuthContext";
import { extractErrorMessage } from "../utils/errorMessage";

export function RegisterPage() {
  const { register } = useAuth();
  const navigate = useNavigate();

  const [form, setForm] = useState({
    firstName: "",
    lastName: "",
    username: "",
    email: "",
    password: "",
  });
  const [error, setError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  function update<K extends keyof typeof form>(key: K, value: string) {
    setForm((f) => ({ ...f, [key]: value }));
  }

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setError(null);
    setIsSubmitting(true);
    try {
      await register(form);
      // Registering now creates a REAL account in SQL Server and logs
      // you straight in (see AuthContext.register) — no separate login
      // step needed, so head straight to the catalog.
      navigate("/");
    } catch (err) {
      setError(extractErrorMessage(err, "Something went wrong creating your account. Please try again."));
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <div className="mx-auto flex min-h-[70vh] max-w-md flex-col justify-center px-6 py-16">
      <h1
        className="text-2xl font-700 text-[var(--color-ink)]"
        style={{ fontFamily: "var(--font-display)" }}
      >
        Create your account
      </h1>
      <p className="mt-1 text-sm text-[var(--color-ink-soft)]">
        Join to start building your cart. This creates a real account in
        your own database.
      </p>

      <form onSubmit={handleSubmit} className="mt-8 flex flex-col gap-4">
        <div className="flex gap-3">
          <label className="flex flex-1 flex-col gap-1.5 text-sm">
            <span className="font-medium text-[var(--color-ink)]">First name</span>
            <input
              required
              value={form.firstName}
              onChange={(e) => update("firstName", e.target.value)}
              className="rounded-lg border border-[var(--color-line)] bg-[var(--color-surface)] px-3 py-2.5 outline-none focus:border-[var(--color-primary)]"
            />
          </label>
          <label className="flex flex-1 flex-col gap-1.5 text-sm">
            <span className="font-medium text-[var(--color-ink)]">Last name</span>
            <input
              required
              value={form.lastName}
              onChange={(e) => update("lastName", e.target.value)}
              className="rounded-lg border border-[var(--color-line)] bg-[var(--color-surface)] px-3 py-2.5 outline-none focus:border-[var(--color-primary)]"
            />
          </label>
        </div>

        <label className="flex flex-col gap-1.5 text-sm">
          <span className="font-medium text-[var(--color-ink)]">Username</span>
          <input
            required
            value={form.username}
            onChange={(e) => update("username", e.target.value)}
            className="rounded-lg border border-[var(--color-line)] bg-[var(--color-surface)] px-3 py-2.5 outline-none focus:border-[var(--color-primary)]"
          />
          <span className="text-xs text-[var(--color-ink-soft)]">Letters, numbers, and underscores only.</span>
        </label>

        <label className="flex flex-col gap-1.5 text-sm">
          <span className="font-medium text-[var(--color-ink)]">Email</span>
          <input
            required
            type="email"
            value={form.email}
            onChange={(e) => update("email", e.target.value)}
            className="rounded-lg border border-[var(--color-line)] bg-[var(--color-surface)] px-3 py-2.5 outline-none focus:border-[var(--color-primary)]"
          />
        </label>

        <label className="flex flex-col gap-1.5 text-sm">
          <span className="font-medium text-[var(--color-ink)]">Password</span>
          <input
            required
            type="password"
            value={form.password}
            onChange={(e) => update("password", e.target.value)}
            className="rounded-lg border border-[var(--color-line)] bg-[var(--color-surface)] px-3 py-2.5 outline-none focus:border-[var(--color-primary)]"
          />
          <span className="text-xs text-[var(--color-ink-soft)]">
            At least 8 characters, with one uppercase letter, one lowercase letter, and one number.
          </span>
        </label>

        {error && <p className="text-sm text-[var(--color-danger)]">{error}</p>}

        <button
          type="submit"
          disabled={isSubmitting}
          className="mt-2 rounded-full bg-[var(--color-ink)] py-2.5 text-sm font-600 text-[var(--color-paper)] transition hover:bg-[var(--color-primary)] disabled:opacity-60"
        >
          {isSubmitting ? "Creating account..." : "Create account"}
        </button>
      </form>

      <p className="mt-6 text-sm text-[var(--color-ink-soft)]">
        Already have an account?{" "}
        <Link to="/login" className="font-600 text-[var(--color-primary)]">
          Log in
        </Link>
      </p>
    </div>
  );
}
