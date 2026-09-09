import type { AxiosError } from "axios";

interface BackendErrorBody {
  errors?: Record<string, string[]>; // FluentValidation shape (400s)
  error?: string;                     // Every other shape: { error: "message" }
}

// The backend (see ExceptionHandlingMiddleware.cs) NEVER sends raw
// exception details, stack traces, or types to the client — every error
// response is a short, safe, human-readable `error` string by design.
// For genuinely unexpected (500-class) failures, the backend's own
// message is deliberately vague ("An unexpected error occurred...") —
// the real cause goes to the backend's server-side log on purpose, not
// the HTTP response.
export function extractErrorMessage(err: unknown, fallback: string): string {
  const axiosErr = err as AxiosError<BackendErrorBody>;
  const data = axiosErr.response?.data;

  if (data?.errors) {
    const firstField = Object.values(data.errors)[0];
    if (firstField?.[0]) return firstField[0];
  }
  if (data?.error) return data.error;

  return fallback;
}
