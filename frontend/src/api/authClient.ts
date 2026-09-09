import axios, { type AxiosRequestConfig } from "axios";

// -----------------------------------------------------------------------
// Points at your real Kartly.API backend (same URL as the Admin
// authClient.ts — see api/adminClient.ts for the parallel admin
// logged-in identity/token). Used ONLY by api/authApi.ts, for:
//   - login / register (now real, persisted in SQL Server)
//   - profile fetch/update
//
// Products still come from the dummy catalog via api/client.ts — this
// file does not touch that.
//
// Token storage keys ("accessToken" / "authUser") are unchanged from
// before, so AuthContext.tsx did not need to change how it reads/writes
// localStorage — only WHERE the token came from changed.
// -----------------------------------------------------------------------
export const authClient = axios.create({
  baseURL: import.meta.env.VITE_ADMIN_API_URL,
  headers: {
    "Content-Type": "application/json",
  },
});

authClient.interceptors.request.use((config) => {
  const token = localStorage.getItem("accessToken");
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

// -----------------------------------------------------------------------
// BUG FIX: "Add to cart" (and every other authenticated action) started
// failing with a silent 401 after ~30 minutes (Jwt:AccessTokenExpiryMinutes
// in appsettings.json) — the exact symptom reported: click Add, get a red
// error, item never lands in the cart.
//
// Root cause: the backend already issues a refreshToken on login/register
// (see authApi.ts), and already exposes POST /api/auth/refresh (see
// AuthController.cs) — but nothing on the frontend ever called it. The old
// interceptor below just wiped localStorage on any 401 and re-threw, while
// leaving React's AuthContext `user` state untouched. So the Navbar kept
// showing "logged in", but every API call kept 401ing forever until a
// manual logout/login.
//
// Fix: on a 401 (other than from the refresh call itself), use the stored
// refreshToken to get a new access/refresh pair, then transparently retry
// the original request. Concurrent 401s share a single in-flight refresh
// call instead of each firing their own. Only if the refresh itself fails
// (refresh token missing/expired/revoked) do we clear storage AND tell the
// rest of the app the session really is gone, via a "kartly:auth-expired"
// event — see AuthContext.tsx, which listens for it and calls logout(),
// updating React state so ProtectedRoute/Navbar reflect reality immediately.
// -----------------------------------------------------------------------
interface RetryableConfig extends AxiosRequestConfig {
  _retry?: boolean;
}

let refreshPromise: Promise<string> | null = null;

async function refreshAccessToken(): Promise<string> {
  const storedRefreshToken = localStorage.getItem("refreshToken");
  if (!storedRefreshToken) {
    throw new Error("No refresh token available");
  }

  // Plain axios call (not `authClient`) so this request never re-enters
  // the response interceptor below and never attaches a now-stale
  // Authorization header — /auth/refresh is [AllowAnonymous] and only
  // needs the refresh token in the body.
  const { data } = await axios.post(
    `${import.meta.env.VITE_ADMIN_API_URL}/auth/refresh`,
    { refreshToken: storedRefreshToken }
  );

  localStorage.setItem("accessToken", data.accessToken);
  localStorage.setItem("refreshToken", data.refreshToken);

  const savedUser = localStorage.getItem("authUser");
  if (savedUser) {
    const merged = { ...JSON.parse(savedUser), accessToken: data.accessToken };
    localStorage.setItem("authUser", JSON.stringify(merged));
  }

  return data.accessToken as string;
}

function clearSessionAndNotify() {
  localStorage.removeItem("accessToken");
  localStorage.removeItem("refreshToken");
  localStorage.removeItem("authUser");
  window.dispatchEvent(new CustomEvent("kartly:auth-expired"));
}

authClient.interceptors.response.use(
  (response) => response,
  async (error) => {
    const originalRequest = error.config as RetryableConfig | undefined;
    const isRefreshCall = originalRequest?.url?.includes("/auth/refresh");

    if (error.response?.status === 401 && originalRequest && !originalRequest._retry && !isRefreshCall) {
      originalRequest._retry = true;
      try {
        // Share one refresh call across every request that 401s at the
        // same time, instead of each triggering its own.
        refreshPromise ??= refreshAccessToken().finally(() => {
          refreshPromise = null;
        });
        const newAccessToken = await refreshPromise;

        originalRequest.headers = {
          ...originalRequest.headers,
          Authorization: `Bearer ${newAccessToken}`,
        };
        return authClient.request(originalRequest);
      } catch {
        clearSessionAndNotify();
        return Promise.reject(error);
      }
    }

    // A 401 on the refresh call itself, or a retry that still failed,
    // means the session is genuinely over.
    if (error.response?.status === 401) {
      clearSessionAndNotify();
    }

    return Promise.reject(error);
  }
);
