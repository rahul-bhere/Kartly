import { useEffect, useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { useCartStore } from "../store/cartStore";
import { QuantityStepper } from "../components/QuantityStepper";
import { EmptyState } from "../components/EmptyState";
import { Loader } from "../components/Loader";
import { formatPrice } from "../utils/format";
import { extractErrorMessage } from "../utils/errorMessage";

export function CartPage() {
  const { cart, isLoading, fetchCart, updateQuantity, removeItem, clearCart, isItemPending, totalPrice } = useCartStore();
  const navigate = useNavigate();
  const [error, setError] = useState<string | null>(null);

  useEffect(() => { fetchCart(); }, [fetchCart]);

  async function handleUpdateQuantity(itemId: string, next: number) {
    setError(null);
    try {
      await updateQuantity(itemId, next);
    } catch (err) {
      // A 409 here (see backend's ExceptionHandlingMiddleware) means
      // this exact item was changed/removed by another in-flight request
      // just before this one landed — refetching shows the true current
      // state rather than leaving the UI stuck on stale data.
      setError(extractErrorMessage(err, "Couldn't update that item. Refreshing your cart..."));
      fetchCart();
    }
  }

  async function handleRemoveItem(itemId: string) {
    setError(null);
    try {
      await removeItem(itemId);
    } catch (err) {
      setError(extractErrorMessage(err, "Couldn't remove that item. Refreshing your cart..."));
      fetchCart();
    }
  }

  if (isLoading && !cart) return <Loader label="Loading your cart" />;

  if (!cart || cart.items.length === 0) {
    return <EmptyState title="Your cart is empty" description="Add a few things from the catalog to see them here." action={<Link to="/" className="rounded-full bg-[var(--color-ink)] px-5 py-2.5 text-sm font-600 text-[var(--color-paper)] transition hover:bg-[var(--color-primary)]">Browse catalog</Link>} />;
  }

  return (
    <div className="mx-auto max-w-4xl px-6 py-10">
      <div className="flex items-center justify-between">
        <h1 className="text-3xl font-700 text-[var(--color-ink)]" style={{ fontFamily: "var(--font-display)" }}>Your cart</h1>
        <button onClick={() => clearCart()} className="text-sm text-[var(--color-ink-soft)] transition hover:text-[var(--color-danger)]">Clear cart</button>
      </div>
      {error && <p className="mt-3 text-sm text-[var(--color-danger)]">{error}</p>}
      <ul className="mt-6 flex flex-col divide-y divide-[var(--color-line)] rounded-2xl border border-[var(--color-line)] bg-[var(--color-surface)]">
        {cart.items.map((item) => {
          const pending = isItemPending(item.id);
          return (
            <li key={item.id} className="flex items-center gap-4 p-4">
              <img src={item.thumbnailUrl ?? "https://placehold.co/80x80?text=Item"} alt={item.productTitle} className="h-20 w-20 rounded-xl object-cover" />
              <div className="flex-1">
                <p className="font-600 text-[var(--color-ink)]">{item.productTitle}</p>
                <p className="font-mono text-sm text-[var(--color-ink-soft)]">{formatPrice(item.unitPrice)} each</p>
              </div>
              <QuantityStepper quantity={item.quantity} onChange={(next) => handleUpdateQuantity(item.id, next)} disabled={pending} />
              <span className="w-20 text-right font-mono text-sm font-600 text-[var(--color-ink)]">{formatPrice(item.lineTotal)}</span>
              <button
                onClick={() => handleRemoveItem(item.id)}
                disabled={pending}
                aria-label={`Remove ${item.productTitle}`}
                className="text-[var(--color-ink-soft)] transition hover:text-[var(--color-danger)] disabled:opacity-40"
              >
                ✕
              </button>
            </li>
          );
        })}
      </ul>
      <div className="mt-8 flex flex-col items-end gap-2">
        <div className="flex w-full max-w-xs items-center justify-between text-sm text-[var(--color-ink-soft)]"><span>Subtotal</span><span className="font-mono">{formatPrice(totalPrice())}</span></div>
        <div className="flex w-full max-w-xs items-center justify-between text-lg font-700 text-[var(--color-ink)]"><span>Total</span><span className="font-mono">{formatPrice(totalPrice())}</span></div>
        <button onClick={() => navigate("/checkout")} className="mt-3 w-full max-w-xs rounded-full bg-[var(--color-ink)] py-3 text-sm font-600 text-[var(--color-paper)] transition hover:bg-[var(--color-primary)]">Checkout</button>
      </div>
    </div>
  );
}
