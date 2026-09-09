import axios from "axios";

// -----------------------------------------------------------------------
// This is the single Axios instance used by the ENTIRE app.
//
// WHY THIS MATTERS FOR THE FUTURE ASP.NET CORE SWAP:
// Every other file (authApi.ts, productApi.ts, cartApi.ts) imports THIS
// file instead of calling axios/fetch directly. When you replace DummyJSON
// with your own backend, you only ever touch:
//   1. VITE_API_URL in .env
//   2. The small request/response shape differences inside api/*.ts
// The components and pages never talk to axios directly, so they never
// need to change.
// -----------------------------------------------------------------------

export const apiClient = axios.create({
  baseURL: import.meta.env.VITE_API_URL,
  headers: {
    "Content-Type": "application/json",
  },
});

// Attach the saved token (if any) to every outgoing request.
// Later, your ASP.NET Core API will likely expect a JWT here too, in the
// same "Authorization: Bearer <token>" format — no change needed.
apiClient.interceptors.request.use((config) => {
  const token = localStorage.getItem("accessToken");
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

// Centralized response error handling. If the token is invalid/expired,
// log the user out automatically. Keeping this in one place means the
// same logic works no matter which backend is behind VITE_API_URL.
apiClient.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response?.status === 401) {
      localStorage.removeItem("accessToken");
      localStorage.removeItem("authUser");
    }
    return Promise.reject(error);
  }
);
