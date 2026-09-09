import { useState } from "react";
import { extractErrorMessage } from "../utils/errorMessage";
import { Link } from "react-router-dom";
import type { Product } from "../types";
import { discountedPrice, formatPrice } from "../utils/format";
import { useCartStore } from "../store/cartStore";
import { useAuth } from "../context/AuthContext";

export function ProductCard({ product }: { product: Product }) {
  const addItem = useCartStore((s) => s.addItem);
  const { isAuthenticated } = useAuth();
  const [isAdding, setIsAdding] = useState(false);
  const [justAdded, setJustAdded] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const finalPrice = discountedPrice(product.price, product.discountPercentage);
  const hasDiscount = product.discountPercentage > 1;

  async function handleAdd() {
    if (!isAuthenticated) {
      window.location.hash = "#/login";
      return;
    }
    setIsAdding(true);
    setError(null);
    try {
      await addItem(product, 1);
      setJustAdded(true);
      setTimeout(() => setJustAdded(false), 1200);
    } catch (err: any) {
      // WHY THIS TRY/CATCH MATTERS: without it, a failed add-to-cart
      // (backend down, validation error, expired session) looked exactly
      // like nothing happening at all — the button just quietly reset.
      // Surfacing the real message is what makes this diagnosable.
      setError(extractErrorMessage(err, "Couldn't add this to your cart."));
      setTimeout(() => setError(null), 4000);
    } finally {
      setIsAdding(false);
    }
  }

  return (
    <div className="group flex flex-col overflow-hidden rounded-2xl border border-[var(--color-line)] bg-[var(--color-surface)] transition hover:shadow-[0_8px_24px_-12px_rgba(28,28,30,0.18)]">
      <Link to={`/products/${product.key}`} className="block overflow-hidden">
        <div className="relative aspect-square overflow-hidden bg-[var(--color-paper)]">
          <img src={product.thumbnail} alt={product.title} loading="lazy" className="h-full w-full object-cover transition duration-300 group-hover:scale-105" />
          {product.source === "real" && (
            <span className="absolute left-2 top-2 rounded-full bg-[var(--color-accent)] px-2 py-0.5 text-[10px] font-600 text-[#1c1c1e]">Store pick</span>
          )}
        </div>
      </Link>
      <div className="flex flex-1 flex-col gap-2 p-4">
        <span className="font-mono text-[10px] uppercase tracking-wider text-[var(--color-ink-soft)]">{product.category}</span>
        <Link to={`/products/${product.key}`}>
          <h3 className="line-clamp-1 font-600 text-[var(--color-ink)]">{product.title}</h3>
        </Link>
        <div className="mt-auto flex items-end justify-between pt-2">
          <div className="price-tag ml-3">
            <div className="flex items-baseline gap-2">
              <span className="text-lg font-600 text-[var(--color-ink)]">{formatPrice(finalPrice)}</span>
              {hasDiscount && <span className="text-xs text-[var(--color-ink-soft)] line-through">{formatPrice(product.price)}</span>}
            </div>
            {hasDiscount && <span className="text-[11px] font-600 text-[var(--color-accent)]">-{Math.round(product.discountPercentage)}%</span>}
          </div>
          <button onClick={handleAdd} disabled={isAdding} className="rounded-full bg-[var(--color-ink)] px-3 py-1.5 text-xs font-600 text-[var(--color-paper)] transition hover:bg-[var(--color-primary)] disabled:opacity-60">
            {justAdded ? "Added ✓" : isAdding ? "..." : "Add"}
          </button>
        </div>
        {error && <p className="text-[11px] text-[var(--color-danger)]">{error}</p>}
      </div>
    </div>
  );
}
