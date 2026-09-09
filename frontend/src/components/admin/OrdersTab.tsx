import { useState } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import * as adminOrderApi from "../../api/adminOrderApi";
import { formatPrice } from "../../utils/format";
import { Loader } from "../Loader";

const STATUS_OPTIONS = ["Pending", "Paid", "Shipped", "Delivered", "Cancelled"];

export function OrdersTab() {
  const queryClient = useQueryClient();
  const [customLabels, setCustomLabels] = useState<Record<string, string>>({});

  const ordersQuery = useQuery({ queryKey: ["admin-orders"], queryFn: adminOrderApi.getAdminOrders });

  const updateStatusMutation = useMutation({
    mutationFn: ({ id, status, label }: { id: string; status: string; label?: string }) =>
      adminOrderApi.updateOrderStatus(id, status, label),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["admin-orders"] });
      queryClient.invalidateQueries({ queryKey: ["my-orders"] });
    },
  });

  const cancelMutation = useMutation({
    mutationFn: (id: string) => adminOrderApi.cancelOrder(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["admin-orders"] });
      queryClient.invalidateQueries({ queryKey: ["my-orders"] });
    },
  });

  if (ordersQuery.isLoading) return <Loader label="Loading orders" />;

  if (ordersQuery.isError) {
    return <p className="rounded-lg border border-dashed border-[var(--color-line)] p-4 text-sm text-[var(--color-ink-soft)]">Couldn't reach the backend. Make sure Kartly.API is running locally.</p>;
  }

  if (ordersQuery.data && ordersQuery.data.length === 0) {
    return <p className="rounded-lg border border-dashed border-[var(--color-line)] p-4 text-sm text-[var(--color-ink-soft)]">No orders yet — orders placed by any shopper (real or dummy-catalog items) will show up here, including who placed them.</p>;
  }

  return (
    <ul className="flex flex-col gap-3">
      {ordersQuery.data?.map((order) => (
        <li key={order.id} className="rounded-2xl border border-[var(--color-line)] bg-[var(--color-surface)] p-4">
          <div className="flex flex-wrap items-start justify-between gap-3">
            <div>
              <p className="font-mono text-xs text-[var(--color-ink-soft)]">#{order.id.slice(0, 8).toUpperCase()} · {new Date(order.createdAt).toLocaleDateString()}</p>
              <p className="mt-1 text-sm font-600 text-[var(--color-ink)]">{order.customerName} <span className="font-mono text-xs text-[var(--color-ink-soft)]">@{order.customerUsername}</span></p>
              <p className="mt-1 text-xs text-[var(--color-ink-soft)]">{order.items.length} item(s) · {order.shippingAddress}</p>
            </div>
            <span className="font-mono font-600 text-[var(--color-ink)]">{formatPrice(order.totalAmount)}</span>
          </div>

          <div className="mt-3 flex flex-wrap items-center gap-2">
            <select
              value={order.status}
              onChange={(e) => updateStatusMutation.mutate({ id: order.id, status: e.target.value, label: customLabels[order.id] })}
              className="input"
            >
              {STATUS_OPTIONS.map((s) => <option key={s} value={s}>{s}</option>)}
            </select>
            <input
              placeholder="Custom status message (optional)"
              defaultValue={order.customStatusLabel ?? ""}
              onChange={(e) => setCustomLabels((prev) => ({ ...prev, [order.id]: e.target.value }))}
              className="input flex-1 min-w-[180px]"
            />
            <button
              onClick={() => updateStatusMutation.mutate({ id: order.id, status: order.status, label: customLabels[order.id] })}
              className="rounded-full border border-[var(--color-line)] px-3 py-1.5 text-xs text-[var(--color-ink)]"
            >
              Save status
            </button>
            {order.status !== "Delivered" && order.status !== "Cancelled" && (
              <button
                onClick={() => cancelMutation.mutate(order.id)}
                className="rounded-full border border-[var(--color-line)] px-3 py-1.5 text-xs text-[var(--color-danger)]"
              >
                Cancel order
              </button>
            )}
          </div>
        </li>
      ))}
    </ul>
  );
}
