import type { ReactNode } from "react";

interface EmptyStateProps {
  title: string;
  description: string;
  action?: ReactNode;
}

export function EmptyState({ title, description, action }: EmptyStateProps) {
  return (
    <div className="mx-auto flex max-w-md flex-col items-center gap-3 py-24 text-center">
      <h2
        className="text-xl font-600 text-[var(--color-ink)]"
        style={{ fontFamily: "var(--font-display)" }}
      >
        {title}
      </h2>
      <p className="text-sm text-[var(--color-ink-soft)]">{description}</p>
      {action}
    </div>
  );
}
