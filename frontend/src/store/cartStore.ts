import { create } from "zustand";
import * as cartApi from "../api/cartApi";
import type { Cart, Product } from "../types";

// WHY NOT localStorage/in-memory: every action here calls the real
// Kartly.API backend, persisted to SQL Server — so an admin can see a
// shopper's actual cart/order history, and the SAME cart follows the
// shopper across devices. Zustand here is just a thin reactive CACHE of
// whatever the backend last returned; the backend is the source of truth.
interface CartState {
  cart: Cart | null;
  isLoading: boolean;
  // WHY THIS EXISTS: clicking a quantity +/- button (or Remove) rapidly,
  // more than once before the first request finishes, used to fire
  // overlapping PUT/DELETE requests for the SAME cart item — each on its
  // own backend request/DbContext, occasionally racing each other into a
  // DbUpdateConcurrencyException server-side (one deletes the row just
  // as another tries to update it). Tracking which item id currently has
  // a request in flight lets the UI disable just THAT item's controls
  // until it resolves, which prevents the race at the source instead of
  // only handling it after the fact on the backend.
  pendingItemIds: Set<string>;
  fetchCart: () => Promise<void>;
  addItem: (product: Product, quantity?: number) => Promise<void>;
  updateQuantity: (cartItemId: string, quantity: number) => Promise<void>;
  removeItem: (cartItemId: string) => Promise<void>;
  clearCart: () => Promise<void>;
  isItemPending: (cartItemId: string) => boolean;
  totalItems: () => number;
  totalPrice: () => number;
}

function withPending(set: any, cartItemId: string, action: () => Promise<Cart>) {
  set((s: CartState) => ({ pendingItemIds: new Set(s.pendingItemIds).add(cartItemId) }));
  return action().then(
    (cart) => {
      set((s: CartState) => {
        const next = new Set(s.pendingItemIds);
        next.delete(cartItemId);
        return { cart, pendingItemIds: next };
      });
    },
    (err) => {
      set((s: CartState) => {
        const next = new Set(s.pendingItemIds);
        next.delete(cartItemId);
        return { pendingItemIds: next };
      });
      throw err;
    }
  );
}

export const useCartStore = create<CartState>()((set, get) => ({
  cart: null,
  isLoading: false,
  pendingItemIds: new Set(),

  fetchCart: async () => {
    set({ isLoading: true });
    try {
      set({ cart: await cartApi.getCart() });
    } finally {
      set({ isLoading: false });
    }
  },

  addItem: async (product, quantity = 1) => {
    set({ cart: await cartApi.addToCart(product, quantity) });
  },

  updateQuantity: async (cartItemId, quantity) => {
    if (quantity < 1) {
      await get().removeItem(cartItemId);
      return;
    }
    await withPending(set, cartItemId, () => cartApi.updateCartItemQuantity(cartItemId, quantity));
  },

  removeItem: async (cartItemId) => {
    await withPending(set, cartItemId, () => cartApi.removeCartItem(cartItemId));
  },

  clearCart: async () => {
    await cartApi.clearCart();
    set({ cart: { id: get().cart?.id ?? "", items: [], totalAmount: 0, totalItems: 0 } });
  },

  isItemPending: (cartItemId) => get().pendingItemIds.has(cartItemId),

  totalItems: () => get().cart?.totalItems ?? 0,
  totalPrice: () => get().cart?.totalAmount ?? 0,
}));
