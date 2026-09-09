import { useState, type FormEvent } from "react";
import { Link, useLocation, useNavigate } from "react-router-dom";
import { useAuth } from "../context/AuthContext";
import { extractErrorMessage } from "../utils/errorMessage";

export function LoginPage() {
  const { login } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const from = (location.state as { from?: Location })?.from?.pathname || "/";

  const [username, setUsername] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setError(null);
    setIsSubmitting(true);
    try {
      await login({ username, password });
      navigate(from, { replace: true });
    } catch (err) {
      setError(extractErrorMessage(err, "Invalid username or password."));
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
        Welcome back
      </h1>
      <p className="mt-1 text-sm text-[var(--color-ink-soft)]">
        Log in to view your cart and check out.
      </p>

      <form onSubmit={handleSubmit} className="mt-8 flex flex-col gap-4">
        <label className="flex flex-col gap-1.5 text-sm">
          <span className="font-medium text-[var(--color-ink)]">Username</span>
          <input
            required
            value={username}
            onChange={(e) => setUsername(e.target.value)}
            className="rounded-lg border border-[var(--color-line)] bg-[var(--color-surface)] px-3 py-2.5 outline-none focus:border-[var(--color-primary)]"
          />
        </label>

        <label className="flex flex-col gap-1.5 text-sm">
          <span className="font-medium text-[var(--color-ink)]">Password</span>
          <input
            required
            type="password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            className="rounded-lg border border-[var(--color-line)] bg-[var(--color-surface)] px-3 py-2.5 outline-none focus:border-[var(--color-primary)]"
            placeholder="••••••••"
          />
        </label>

        {error && <p className="text-sm text-[var(--color-danger)]">{error}</p>}

        <button
          type="submit"
          disabled={isSubmitting}
          className="mt-2 rounded-full bg-[var(--color-ink)] py-2.5 text-sm font-600 text-[var(--color-paper)] transition hover:bg-[var(--color-primary)] disabled:opacity-60"
        >
          {isSubmitting ? "Logging in..." : "Log in"}
        </button>
      </form>

      <p className="mt-6 text-sm text-[var(--color-ink-soft)]">
        Don&apos;t have an account?{" "}
        <Link to="/register" className="font-600 text-[var(--color-primary)]">
          Sign up
        </Link>
      </p>
    </div>
  );
}
