import { useState, type FormEvent } from "react";

export function ContactPage() {
  const [form, setForm] = useState({ name: "", email: "", message: "" });
  const [sent, setSent] = useState(false);

  function handleSubmit(e: FormEvent) {
    e.preventDefault();
    // Simulated — this is a portfolio project with no real inbox behind
    // it. Wiring this to a real email service (e.g. an ASP.NET Core
    // endpoint using SendGrid/SES) is a natural next step.
    setSent(true);
  }

  return (
    <div className="mx-auto max-w-lg px-6 py-14">
      <h1 className="text-3xl font-700 text-[var(--color-ink)]" style={{ fontFamily: "var(--font-display)" }}>Contact us</h1>
      <p className="mt-2 text-sm text-[var(--color-ink-soft)]">This form is simulated for demo purposes — no message is actually sent anywhere.</p>

      {sent ? (
        <div className="mt-6 rounded-2xl border border-[var(--color-success)]/30 bg-[var(--color-success)]/10 p-4 text-sm text-[var(--color-success)]">
          Thanks! (Simulated — nothing was actually sent.)
        </div>
      ) : (
        <form onSubmit={handleSubmit} className="mt-6 flex flex-col gap-4">
          <label className="flex flex-col gap-1.5 text-sm">
            <span className="font-medium text-[var(--color-ink)]">Name</span>
            <input required value={form.name} onChange={(e) => setForm((f) => ({ ...f, name: e.target.value }))} className="rounded-lg border border-[var(--color-line)] bg-[var(--color-surface)] px-3 py-2.5 outline-none focus:border-[var(--color-primary)]" />
          </label>
          <label className="flex flex-col gap-1.5 text-sm">
            <span className="font-medium text-[var(--color-ink)]">Email</span>
            <input required type="email" value={form.email} onChange={(e) => setForm((f) => ({ ...f, email: e.target.value }))} className="rounded-lg border border-[var(--color-line)] bg-[var(--color-surface)] px-3 py-2.5 outline-none focus:border-[var(--color-primary)]" />
          </label>
          <label className="flex flex-col gap-1.5 text-sm">
            <span className="font-medium text-[var(--color-ink)]">Message</span>
            <textarea required rows={4} value={form.message} onChange={(e) => setForm((f) => ({ ...f, message: e.target.value }))} className="rounded-lg border border-[var(--color-line)] bg-[var(--color-surface)] px-3 py-2.5 outline-none focus:border-[var(--color-primary)]" />
          </label>
          <button type="submit" className="rounded-full bg-[var(--color-ink)] py-2.5 text-sm font-600 text-[var(--color-paper)] transition hover:bg-[var(--color-primary)]">Send message</button>
        </form>
      )}
    </div>
  );
}
