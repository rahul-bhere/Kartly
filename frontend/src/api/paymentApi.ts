import { authClient } from "./authClient";
import type { PaymentMethod } from "../types";

// SIMULATED payment only — no real card data is ever collected or sent.
export async function pay(orderId: string, method: PaymentMethod): Promise<void> {
  await authClient.post("/payments", { orderId, method });
}
