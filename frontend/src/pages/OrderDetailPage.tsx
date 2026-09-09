import { Link, useParams } from "react-router-dom";
import { useQuery, useQueryClient, useMutation } from "@tanstack/react-query";
import * as orderApi from "../api/orderApi";
import { formatPrice } from "../utils/format";
import { EmptyState } from "../components/EmptyState";
import { Loader } from "../components/Loader";

const STATUS_COLORS: Record<string, string> = { Pending: "text-[var(--color-accent)]", Paid: "text-[var(--color-success)]", Shipped: "text-[var(--color-primary)]", Delivered: "text-[var(--color-success)]", Cancelled: "text-[var(--color-danger)]" };

export function OrderDetailPage() {
  const { id } = useParams<{ id: string }>();
  const queryClient = useQueryClient();

  const { data: order, isLoading } = useQuery({ queryKey: ["order", id], queryFn: () => orderApi.getOrderById(id!), enabled: !!id });

  const cancelMutation = useMutation({
    mutationFn: () => orderApi.cancelOrder(id!),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["order", id] });
      queryClient.invalidateQueries({ queryKey: ["my-orders"] });
    },
  });

  if (isLoading) return <Loader label="Loading order" />;
  if (!order) {
    return <EmptyState title="Order not found" description="This order doesn't exist, or isn't yours." action={<Link to="/orders" className="text-sm font-600 text-[var(--color-primary)]">View your orders</Link>} />;
  }

  const canCancel = order.status !== "Delivered" && order.status !== "Cancelled";

  return (
    <div className="mx-auto max-w-2xl px-6 py-10">
      {order.status !== "Cancelled" && (
        <div className="rounded-2xl border border-[var(--color-success)]/30 bg-[var(--color-success)]/10 p-4 text-sm text-[var(--color-success)]">✓ Order placed successfully — saved to the database and visible to the store admin.</div>
      )}
      <h1 className="mt-6 text-2xl font-700 text-[var(--color-ink)]" style={{ fontFamily: "var(--font-display)" }}>Order #{order.id.slice(0, 8).toUpperCase()}</h1>
      <p className="mt-1 text-sm text-[var(--color-ink-soft)]">Placed on {new Date(order.createdAt).toLocaleString()}</p>
      <p className={`mt-2 text-sm font-600 ${STATUS_COLORS[order.status]}`}>Status: {order.customStatusLabel ?? order.status}</p>
      <div className="mt-6 rounded-2xl border border-[var(--color-line)] bg-[var(--color-surface)] p-4">
        <h2 className="font-600 text-[var(--color-ink)]">Items</h2>
        <ul className="mt-3 flex flex-col gap-2 text-sm">
          {order.items.map((item, i) => (
            <li key={i} className="flex justify-between text-[var(--color-ink-soft)]"><span>{item.productTitle} × {item.quantity}</span><span className="font-mono">{formatPrice(item.lineTotal)}</span></li>
          ))}
        </ul>
        <div className="mt-3 flex justify-between border-t border-[var(--color-line)] pt-3 font-600 text-[var(--color-ink)]"><span>Total</span><span className="font-mono">{formatPrice(order.totalAmount)}</span></div>
      </div>
      <div className="mt-4 rounded-2xl border border-[var(--color-line)] bg-[var(--color-surface)] p-4 text-sm">
        <p className="text-xs uppercase tracking-wide text-[var(--color-ink-soft)]">Shipping to</p>
        <p className="mt-1 text-[var(--color-ink)]">{order.shippingAddress}</p>
      </div>
      <div className="mt-6 flex gap-3">
        <Link to="/" className="rounded-full bg-[var(--color-ink)] px-5 py-2.5 text-sm font-600 text-[var(--color-paper)] transition hover:bg-[var(--color-primary)]">Continue shopping</Link>
        {canCancel && (
          <button onClick={() => cancelMutation.mutate()} disabled={cancelMutation.isPending} className="rounded-full border border-[var(--color-line)] px-5 py-2.5 text-sm font-600 text-[var(--color-danger)] transition hover:border-[var(--color-danger)] disabled:opacity-60">
            {cancelMutation.isPending ? "Cancelling..." : "Cancel this order"}
          </button>
        )}
      </div>
    </div>
  );
}
