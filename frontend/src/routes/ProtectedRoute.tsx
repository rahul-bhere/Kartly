import { Navigate, Outlet, useLocation } from "react-router-dom";
import { useAuth } from "../context/AuthContext";

export function ProtectedRoute() {
  const { isAuthenticated, isLoading } = useAuth();
  const location = useLocation();

  if (isLoading) return null; // could render a full-page loader here

  if (!isAuthenticated) {
    // Send the user to login, remembering where they were headed.
    return <Navigate to="/login" state={{ from: location }} replace />;
  }

  return <Outlet />;
}
