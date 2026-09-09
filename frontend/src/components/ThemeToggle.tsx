import { useTheme } from "../context/ThemeContext";

const LABELS: Record<string, string> = { light: "Light", dark: "Dark", darkest: "Darkest" };

export function ThemeToggle() {
  const { theme, cycleTheme } = useTheme();
  return (
    <button
      onClick={cycleTheme}
      title={`Theme: ${LABELS[theme]} — click to cycle`}
      className="flex h-8 items-center gap-1.5 rounded-full border border-[var(--color-line)] px-3 text-xs font-600 text-[var(--color-ink)] transition hover:border-[var(--color-primary)]"
    >
      <span
        className="h-3 w-3 rounded-full border border-[var(--color-line)]"
        style={{ background: theme === "light" ? "#fff" : theme === "dark" ? "#333" : "#000" }}
      />
      {LABELS[theme]}
    </button>
  );
}
