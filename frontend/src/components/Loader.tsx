export function Loader({ label = "Loading..." }: { label?: string }) {
  return (
    <div className="flex flex-col items-center justify-center gap-3 py-24 text-[var(--color-ink-soft)]">
      <div
        className="h-8 w-8 animate-spin rounded-full border-2 border-[var(--color-line)] border-t-[var(--color-primary)]"
        role="status"
        aria-label={label}
      />
      <p className="font-mono text-xs uppercase tracking-wider">{label}</p>
    </div>
  );
}
