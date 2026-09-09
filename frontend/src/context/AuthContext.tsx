import { createContext, useContext, useEffect, useState, type ReactNode } from "react";
import { useQueryClient } from "@tanstack/react-query";
import * as authApi from "../api/authApi";
import { useCartStore } from "../store/cartStore";
import type { AuthUser, ChangePasswordPayload, LoginPayload, RegisterPayload } from "../types";

// ONE auth context for everyone — a "user" and an "admin" are the SAME
// kind of account, just with a different Role claim in the JWT.
interface AuthContextValue {
  user: AuthUser | null;
  isAuthenticated: boolean;
  isAdmin: boolean;
  isLoading: boolean;
  login: (payload: LoginPayload) => Promise<void>;
  register: (payload: RegisterPayload) => Promise<void>;
  updateProfile: (payload: { firstName: string; lastName: string; email: string }) => Promise<void>;
  changePassword: (payload: ChangePasswordPayload) => Promise<void>;
  logout: () => void;
}

const AuthContext = createContext<AuthContextValue | undefined>(undefined);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<AuthUser | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const queryClient = useQueryClient();

  useEffect(() => {
    const savedUser = localStorage.getItem("authUser");
    if (savedUser) setUser(JSON.parse(savedUser));
    setIsLoading(false);
  }, []);

  // WHY THIS EXISTS: authClient.ts silently refreshes an expired access
  // token behind the scenes using the stored refresh token. But if that
  // refresh token is ALSO expired/revoked (e.g. the user left the tab open
  // for days), authClient.ts clears localStorage and dispatches this event
  // instead of updating React state directly (a plain module can't call a
  // hook). Without this listener, `user` stayed populated in memory even
  // though every API call was 401ing — the Navbar kept showing "logged in"
  // and buttons like "Add to cart" kept failing with no way to recover
  // short of a manual logout/login. Listening here keeps isAuthenticated
  // truthful, which lets ProtectedRoute/Navbar react immediately.
  useEffect(() => {
    function handleAuthExpired() {
      setUser(null);
      resetClientCaches();
    }
    window.addEventListener("kartly:auth-expired", handleAuthExpired);
    return () => window.removeEventListener("kartly:auth-expired", handleAuthExpired);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  // -----------------------------------------------------------------------
  // WHY THIS EXISTS: React Query caches server responses by key — e.g.
  // ["my-orders"] — regardless of WHICH user was logged in when that data
  // was fetched. Without clearing the cache on every session change, a
  // second account logging in on the same browser tab could briefly see
  // the PREVIOUS user's cached orders/cart until something happened to
  // naturally trigger a refetch. Wiping the cache (and the cart store,
  // which is a separate Zustand cache of the same kind) on every
  // login/register/logout closes that gap — every new session starts
  // with a clean slate and refetches everything fresh from the backend,
  // which still enforces per-user ownership server-side regardless.
  // -----------------------------------------------------------------------
  function resetClientCaches() {
    queryClient.clear();
    useCartStore.setState({ cart: null });
  }

  function persistSession(authUser: AuthUser) {
    resetClientCaches();
    localStorage.setItem("accessToken", authUser.accessToken);
    localStorage.setItem("authUser", JSON.stringify(authUser));
    setUser(authUser);
  }

  async function login(payload: LoginPayload) {
    persistSession(await authApi.login(payload));
  }

  async function register(payload: RegisterPayload) {
    persistSession(await authApi.register(payload));
  }

  async function updateProfile(payload: { firstName: string; lastName: string; email: string }) {
    if (!user) throw new Error("Not logged in");
    const updated = await authApi.updateProfile(payload);
    const merged: AuthUser = { ...user, ...updated };
    localStorage.setItem("authUser", JSON.stringify(merged));
    setUser(merged);
  }

  async function changePassword(payload: ChangePasswordPayload) {
    await authApi.changePassword(payload);
  }

  function logout() {
    localStorage.removeItem("accessToken");
    localStorage.removeItem("refreshToken");
    localStorage.removeItem("authUser");
    setUser(null);
    resetClientCaches();
  }

  return (
    <AuthContext.Provider
      value={{
        user,
        isAuthenticated: !!user,
        isAdmin: user?.role === "Admin",
        isLoading,
        login,
        register,
        updateProfile,
        changePassword,
        logout,
      }}
    >
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth() {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error("useAuth must be used within an AuthProvider");
  return ctx;
}
