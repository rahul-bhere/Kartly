import { authClient } from "./authClient";
import type { Order } from "../types";

// Matches Kartly.API's OrdersController Admin-only endpoints. Orders now
// come from the SAME real checkout flow shoppers use — every order a
// shopper places (even from dummy-catalog items) shows up here, with who
// placed it (customerName/customerUsername).
export async function getAdminOrders(): Promise<Order[]> {
  const { data } = await authClient.get("/orders");
  return data as Order[];
}

export async function updateOrderStatus(id: string, status: string, customStatusLabel?: string): Promise<Order> {
  const { data } = await authClient.put(`/orders/${id}/status`, { status, customStatusLabel });
  return data as Order;
}

export async function cancelOrder(id: string): Promise<Order> {
  const { data } = await authClient.put(`/orders/${id}/cancel`, {});
  return data as Order;
}
