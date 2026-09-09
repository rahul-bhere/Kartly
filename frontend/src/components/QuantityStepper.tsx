interface QuantityStepperProps {
  quantity: number;
  onChange: (next: number) => void;
  disabled?: boolean;
}

export function QuantityStepper({ quantity, onChange, disabled = false }: QuantityStepperProps) {
  return (
    <div className="flex items-center rounded-full border border-[var(--color-line)]">
      <button
        aria-label="Decrease quantity"
        onClick={() => onChange(quantity - 1)}
        disabled={disabled}
        className="h-8 w-8 rounded-full text-[var(--color-ink)] transition hover:bg-[var(--color-paper)] disabled:opacity-40"
      >
        −
      </button>
      <span className="w-8 text-center font-mono text-sm">{quantity}</span>
      <button
        aria-label="Increase quantity"
        onClick={() => onChange(quantity + 1)}
        disabled={disabled}
        className="h-8 w-8 rounded-full text-[var(--color-ink)] transition hover:bg-[var(--color-paper)] disabled:opacity-40"
      >
        +
      </button>
    </div>
  );
}
