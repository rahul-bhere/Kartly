import { Link } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import * as orderApi from "../api/orderApi";
import { formatPrice } from "../utils/format";
import { EmptyState } from "../components/EmptyState";
import { Loader } from "../components/Loader";

const STATUS_COLORS: Record<string, string> = { Pending: "text-[var(--color-accent)]", Paid: "text-[var(--color-success)]", Shipped: "text-[var(--color-primary)]", Delivered: "text-[var(--color-success)]", Cancelled: "text-[var(--color-danger)]" };

export function OrdersPage() {
  const { data: orders, isLoading } = useQuery({ queryKey: ["my-orders"], queryFn: orderApi.getMyOrders });

  if (isLoading) return <Loader label="Loading your orders" />;
  if (!orders || orders.length === 0) {
    return <EmptyState title="No orders yet" description="Orders you place will show up here — and your admin can see them too." action={<Link to="/" className="rounded-full bg-[var(--color-ink)] px-5 py-2.5 text-sm font-600 text-[var(--color-paper)] transition hover:bg-[var(--color-primary)]">Browse catalog</Link>} />;
  }

  return (
    <div className="mx-auto max-w-3xl px-6 py-10">
      <h1 className="text-3xl font-700 text-[var(--color-ink)]" style={{ fontFamily: "var(--font-display)" }}>Your orders</h1>
      <ul className="mt-6 flex flex-col gap-3">
        {orders.map((order) => (
          <li key={order.id}>
            <Link to={`/orders/${order.id}`} className="flex items-center justify-between rounded-2xl border border-[var(--color-line)] bg-[var(--color-surface)] p-4 transition hover:border-[var(--color-primary)]">
              <div>
                <p className="font-mono text-xs text-[var(--color-ink-soft)]">#{order.id.slice(0, 8).toUpperCase()} · {new Date(order.createdAt).toLocaleDateString()}</p>
                <p className="mt-1 text-sm text-[var(--color-ink)]">{order.items.length} item{order.items.length > 1 ? "s" : ""}</p>
              </div>
              <div className="text-right">
                <p className="font-mono font-600 text-[var(--color-ink)]">{formatPrice(order.totalAmount)}</p>
                <p className={`text-xs font-600 ${STATUS_COLORS[order.status]}`}>{order.customStatusLabel ?? order.status}</p>
              </div>
            </Link>
          </li>
        ))}
      </ul>
    </div>
  );
}
