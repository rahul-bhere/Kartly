import { useState, type FormEvent } from "react";
import { extractErrorMessage } from "../utils/errorMessage";
import { useNavigate } from "react-router-dom";
import { useCartStore } from "../store/cartStore";
import * as orderApi from "../api/orderApi";
import * as paymentApi from "../api/paymentApi";
import { formatPrice } from "../utils/format";
import { validate, minLength } from "../utils/validation";
import type { FieldErrors } from "../utils/validation";
import type { PaymentMethod } from "../types";
import { EmptyState } from "../components/EmptyState";

const PAYMENT_METHODS: { value: PaymentMethod; label: string }[] = [
  { value: "CreditCard", label: "Credit / Debit Card" },
  { value: "PayPal", label: "PayPal" },
  { value: "CashOnDelivery", label: "Cash on Delivery" },
];

export function CheckoutPage() {
  const { cart, clearCart, totalPrice } = useCartStore();
  const navigate = useNavigate();
  const [address, setAddress] = useState("");
  const [method, setMethod] = useState<PaymentMethod>("CreditCard");
  const [cardNumber, setCardNumber] = useState("");
  const [errors, setErrors] = useState<FieldErrors>({});
  const [isProcessing, setIsProcessing] = useState(false);
  const [serverError, setServerError] = useState<string | null>(null);

  if (!cart || cart.items.length === 0) {
    return <EmptyState title="Nothing to check out" description="Your cart is empty." />;
  }

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setServerError(null);
    const fieldErrors = validate({
      address: () => minLength(address, 10) || "Enter a full shipping address (10+ characters)",
      ...(method === "CreditCard" ? { cardNumber: () => cardNumber.replace(/\s/g, "").length >= 12 || "Enter a card number (simulation only)" } : {}),
    });
    setErrors(fieldErrors);
    if (Object.keys(fieldErrors).length > 0) return;

    setIsProcessing(true);
    try {
      const order = await orderApi.checkout(address);
      await paymentApi.pay(order.id, method);
      await clearCart();
      navigate(`/orders/${order.id}`);
    } catch (err: any) {
      setServerError(extractErrorMessage(err, "Something went wrong placing your order. Please try again."));
    } finally {
      setIsProcessing(false);
    }
  }

  return (
    <div className="mx-auto max-w-3xl px-6 py-10">
      <h1 className="text-3xl font-700 text-[var(--color-ink)]" style={{ fontFamily: "var(--font-display)" }}>Checkout</h1>
      <div className="mt-6 grid grid-cols-1 gap-8 md:grid-cols-[1.3fr_1fr]">
        <form onSubmit={handleSubmit} className="flex flex-col gap-5">
          <fieldset className="flex flex-col gap-1.5">
            <label className="text-sm font-medium text-[var(--color-ink)]">Shipping address</label>
            <textarea value={address} onChange={(e) => setAddress(e.target.value)} rows={3} placeholder="Street, city, state, ZIP" className="rounded-lg border border-[var(--color-line)] bg-[var(--color-surface)] px-3 py-2.5 text-sm outline-none focus:border-[var(--color-primary)]" />
            {errors.address && <span className="text-xs text-[var(--color-danger)]">{errors.address}</span>}
          </fieldset>
          <fieldset className="flex flex-col gap-2">
            <legend className="text-sm font-medium text-[var(--color-ink)]">Payment method</legend>
            {PAYMENT_METHODS.map((pm) => (
              <label key={pm.value} className="flex cursor-pointer items-center gap-3 rounded-lg border border-[var(--color-line)] bg-[var(--color-surface)] px-3 py-2.5 text-sm">
                <input type="radio" name="paymentMethod" checked={method === pm.value} onChange={() => setMethod(pm.value)} />
                {pm.label}
              </label>
            ))}
          </fieldset>
          {method === "CreditCard" && (
            <fieldset className="flex flex-col gap-1.5">
              <label className="text-sm font-medium text-[var(--color-ink)]">Card number (simulation only)</label>
              <input value={cardNumber} onChange={(e) => setCardNumber(e.target.value)} placeholder="4242 4242 4242 4242" className="rounded-lg border border-[var(--color-line)] bg-[var(--color-surface)] px-3 py-2.5 text-sm font-mono outline-none focus:border-[var(--color-primary)]" />
              {errors.cardNumber && <span className="text-xs text-[var(--color-danger)]">{errors.cardNumber}</span>}
            </fieldset>
          )}
          {serverError && <p className="text-sm text-[var(--color-danger)]">{serverError}</p>}
          <div className="rounded-lg border border-dashed border-[var(--color-line)] p-3 text-xs text-[var(--color-ink-soft)]">
            This order and its "payment" are real rows in your SQL Server database — visible to the admin. Only the CHARGE is simulated; no real card data is ever sent anywhere.
          </div>
          <button type="submit" disabled={isProcessing} className="rounded-full bg-[var(--color-ink)] py-3 text-sm font-600 text-[var(--color-paper)] transition hover:bg-[var(--color-primary)] disabled:opacity-60">
            {isProcessing ? "Processing payment..." : `Pay ${formatPrice(totalPrice())}`}
          </button>
        </form>
        <div className="h-fit rounded-2xl border border-[var(--color-line)] bg-[var(--color-surface)] p-4">
          <h2 className="font-600 text-[var(--color-ink)]">Order summary</h2>
          <ul className="mt-3 flex flex-col gap-2 text-sm">
            {cart.items.map((item) => (
              <li key={item.id} className="flex justify-between text-[var(--color-ink-soft)]">
                <span className="line-clamp-1 pr-2">{item.productTitle} × {item.quantity}</span>
                <span className="font-mono">{formatPrice(item.lineTotal)}</span>
              </li>
            ))}
          </ul>
          <div className="mt-3 flex justify-between border-t border-[var(--color-line)] pt-3 font-600 text-[var(--color-ink)]"><span>Total</span><span className="font-mono">{formatPrice(totalPrice())}</span></div>
        </div>
      </div>
    </div>
  );
}
