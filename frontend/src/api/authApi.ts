import { authClient } from "./authClient";
import type { AuthUser, LoginPayload, RegisterPayload, User } from "../types";

// -----------------------------------------------------------------------
// LOGIN / REGISTER NOW HIT YOUR REAL BACKEND (Kartly.API + SQL Server) —
// no longer the dummy API. A user registered here is a real row in the
// Users table and can log back in with the same credentials, unlike the
// old DummyJSON-backed version.
//
// Backend shapes (Kartly.Application/DTOs/Auth/AuthResponseDto.cs):
//   { accessToken, refreshToken, accessTokenExpiresAt, user: UserDto }
// This file's job is to adapt that shape into the flat `AuthUser` type
// the rest of the frontend already expects — see the two `{ ...data.user,
// accessToken: ... }` lines below. That adapter is the ONLY reason this
// file needs backend-specific code at all.
// -----------------------------------------------------------------------

export async function login(payload: LoginPayload): Promise<AuthUser> {
  const { data } = await authClient.post("/auth/login", {
    username: payload.username,
    password: payload.password,
  });

  // Real backend also issues a refresh token — stash it so a future
  // "silent refresh on 401" feature has something to use. Not wired into
  // the request interceptor yet (see authClient.ts) — a natural next step.
  localStorage.setItem("refreshToken", data.refreshToken);

  return { ...data.user, id: data.user.id, accessToken: data.accessToken } as AuthUser;
}

export async function register(payload: RegisterPayload): Promise<AuthUser> {
  // Kartly.API's /auth/register ALSO returns a full AuthResponseDto (see
  // AuthController.cs), so registering logs the user in immediately —
  // no separate login step needed, unlike the old dummy-API version.
  const { data } = await authClient.post("/auth/register", {
    firstName: payload.firstName,
    lastName: payload.lastName,
    username: payload.username,
    email: payload.email,
    password: payload.password,
  });

  localStorage.setItem("refreshToken", data.refreshToken);

  return { ...data.user, accessToken: data.accessToken } as AuthUser;
}

export async function getCurrentUser(): Promise<User> {
  const { data } = await authClient.get("/users/me");
  return data as User;
}

export async function updateProfile(payload: { firstName: string; lastName: string; email: string }): Promise<User> {
  // Kartly.API's PUT /users/me infers WHICH user from the JWT itself —
  // no id needed in the URL, unlike the old dummy-API version which
  // needed /users/{id}.
  const { data } = await authClient.put("/users/me", payload);
  return data as User;
}

export async function changePassword(payload: { currentPassword: string; newPassword: string }): Promise<void> {
  await authClient.post("/users/me/change-password", payload);
}
