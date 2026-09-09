import { useState } from "react";
import { extractErrorMessage } from "../utils/errorMessage";
import { useParams, Link, useNavigate } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import * as productApi from "../api/productApi";
import { Loader } from "../components/Loader";
import { EmptyState } from "../components/EmptyState";
import { QuantityStepper } from "../components/QuantityStepper";
import { discountedPrice, formatPrice } from "../utils/format";
import { useCartStore } from "../store/cartStore";
import { useAuth } from "../context/AuthContext";

export function ProductDetailPage() {
  const { key } = useParams<{ key: string }>();
  const addItem = useCartStore((s) => s.addItem);
  const { isAuthenticated } = useAuth();
  const navigate = useNavigate();
  const [quantity, setQuantity] = useState(1);
  const [justAdded, setJustAdded] = useState(false);
  const [isAdding, setIsAdding] = useState(false);
  const [addError, setAddError] = useState<string | null>(null);

  const { data: product, isLoading, isError } = useQuery({
    queryKey: ["product", key],
    queryFn: () => productApi.getProductByKey(key!),
    enabled: !!key,
  });

  if (isLoading) return <Loader label="Loading product" />;
  if (isError || !product) {
    return <EmptyState title="Product not found" description="It may have been removed or the link is incorrect." action={<Link to="/" className="text-sm font-600 text-[var(--color-primary)]">Back to catalog</Link>} />;
  }

  const finalPrice = discountedPrice(product.price, product.discountPercentage);

  async function handleAdd() {
    if (!isAuthenticated) { navigate("/login"); return; }
    setIsAdding(true);
    setAddError(null);
    try {
      await addItem(product!, quantity);
      setJustAdded(true);
      setTimeout(() => setJustAdded(false), 1600);
    } catch (err: any) {
      setAddError(extractErrorMessage(err, "Couldn't add this to your cart. Please try again."));
    } finally {
      setIsAdding(false);
    }
  }

  return (
    <div className="mx-auto max-w-5xl px-6 py-10">
      <Link to="/" className="text-sm text-[var(--color-ink-soft)] hover:text-[var(--color-ink)]">← Back to catalog</Link>
      <div className="mt-6 grid grid-cols-1 gap-10 md:grid-cols-2">
        <div className="aspect-square overflow-hidden rounded-2xl border border-[var(--color-line)] bg-[var(--color-surface)]">
          <img src={product.thumbnail} alt={product.title} className="h-full w-full object-cover" />
        </div>
        <div className="flex flex-col gap-4">
          <span className="font-mono text-xs uppercase tracking-wider text-[var(--color-ink-soft)]">
            {product.category} • {product.brand}
            {product.source === "real" && <span className="ml-2 rounded-full bg-[var(--color-accent)] px-2 py-0.5 text-[10px] font-600 text-[#1c1c1e]">Store pick</span>}
          </span>
          <h1 className="text-3xl font-700 text-[var(--color-ink)]" style={{ fontFamily: "var(--font-display)" }}>{product.title}</h1>
          <div className="price-tag ml-3 flex items-baseline gap-3">
            <span className="text-2xl font-600 text-[var(--color-ink)]">{formatPrice(finalPrice)}</span>
            {product.discountPercentage > 1 && <span className="text-sm text-[var(--color-ink-soft)] line-through">{formatPrice(product.price)}</span>}
          </div>
          <p className="leading-relaxed text-[var(--color-ink-soft)]">{product.description}</p>
          <div className="flex items-center gap-2 text-sm">
            <span className={product.stock > 0 ? "text-[var(--color-success)]" : "text-[var(--color-danger)]"}>
              {product.stock > 0 ? `In stock (${product.stock})` : "Out of stock"}
            </span>
            <span className="text-[var(--color-ink-soft)]">•</span>
            <span className="text-[var(--color-ink-soft)]">Rated {product.rating.toFixed(1)} / 5</span>
          </div>
          <div className="mt-2 flex items-center gap-4">
            <QuantityStepper quantity={quantity} onChange={setQuantity} />
            <button onClick={handleAdd} disabled={product.stock === 0 || isAdding} className="flex-1 rounded-full bg-[var(--color-ink)] py-3 text-sm font-600 text-[var(--color-paper)] transition hover:bg-[var(--color-primary)] disabled:opacity-50">
              {justAdded ? "Added to cart ✓" : isAdding ? "Adding..." : "Add to cart"}
            </button>
          </div>
          {addError && <p className="text-sm text-[var(--color-danger)]">{addError}</p>}
        </div>
      </div>
    </div>
  );
}
