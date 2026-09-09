import { authClient } from "./authClient";
import type { Cart, Product } from "../types";

// The cart is REAL now — persisted server-side in SQL Server, for BOTH
// dummy-catalog items and admin-created (real) products.
export async function getCart(): Promise<Cart> {
  const { data } = await authClient.get("/cart");
  return data as Cart;
}

export async function addToCart(product: Product, quantity = 1): Promise<Cart> {
  const payload =
    product.source === "real"
      ? { productId: product.id, quantity }
      : {
          externalRef: `dummy-${product.id}`,
          title: product.title,
          unitPrice: product.price,
          thumbnailUrl: product.thumbnail,
          quantity,
        };
  const { data } = await authClient.post("/cart/items", payload);
  return data as Cart;
}

export async function updateCartItemQuantity(cartItemId: string, quantity: number): Promise<Cart> {
  const { data } = await authClient.put(`/cart/items/${cartItemId}`, { quantity });
  return data as Cart;
}

export async function removeCartItem(cartItemId: string): Promise<Cart> {
  const { data } = await authClient.delete(`/cart/items/${cartItemId}`);
  return data as Cart;
}

export async function clearCart(): Promise<void> {
  await authClient.delete("/cart");
}
