import { authClient } from "./authClient";
import type { AdminUser } from "../types";

// Matches Kartly.API's UsersController.
export async function getAdminUsers(): Promise<AdminUser[]> {
  const { data } = await authClient.get("/users");
  return data as AdminUser[];
}

export async function setUserActive(id: string, role: string, isActive: boolean): Promise<AdminUser> {
  const { data } = await authClient.put(`/users/${id}`, { role, isActive });
  return data as AdminUser;
}

export async function deleteAdminUser(id: string): Promise<void> {
  await authClient.delete(`/users/${id}`);
}
