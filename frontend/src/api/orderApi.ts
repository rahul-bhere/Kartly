import { authClient } from "./authClient";
import type { Order } from "../types";

export async function checkout(shippingAddress: string): Promise<Order> {
  const { data } = await authClient.post("/orders/checkout", { shippingAddress });
  return data as Order;
}

export async function getMyOrders(): Promise<Order[]> {
  const { data } = await authClient.get("/orders/mine");
  return data as Order[];
}

export async function getOrderById(id: string): Promise<Order> {
  const { data } = await authClient.get(`/orders/${id}`);
  return data as Order;
}

export async function cancelOrder(id: string): Promise<Order> {
  const { data } = await authClient.put(`/orders/${id}/cancel`, {});
  return data as Order;
}
