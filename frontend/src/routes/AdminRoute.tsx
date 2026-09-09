import { Navigate, Outlet } from "react-router-dom";
import { useAuth } from "../context/AuthContext";

// Replaces the old separate AdminProtectedRoute + AdminAuthContext.
// There's now ONE login/session for everyone — this guard just checks
// the SAME logged-in user's role.
export function AdminRoute() {
  const { isAuthenticated, isLoading, isAdmin } = useAuth();
  if (isLoading) return null;
  if (!isAuthenticated) return <Navigate to="/login" replace />;
  if (!isAdmin) return <Navigate to="/" replace />;
  return <Outlet />;
}
