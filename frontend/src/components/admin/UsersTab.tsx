import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import * as adminUserApi from "../../api/adminUserApi";
import { Loader } from "../Loader";

export function UsersTab() {
  const queryClient = useQueryClient();

  const usersQuery = useQuery({
    queryKey: ["admin-users"],
    queryFn: adminUserApi.getAdminUsers,
  });

  const toggleActiveMutation = useMutation({
    mutationFn: ({ id, role, isActive }: { id: string; role: string; isActive: boolean }) =>
      adminUserApi.setUserActive(id, role, isActive),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["admin-users"] }),
  });

  const deleteMutation = useMutation({
    mutationFn: adminUserApi.deleteAdminUser,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["admin-users"] }),
  });

  if (usersQuery.isLoading) return <Loader label="Loading users" />;

  if (usersQuery.isError) {
    return (
      <p className="rounded-lg border border-dashed border-[var(--color-line)] p-4 text-sm text-[var(--color-ink-soft)]">
        Couldn't reach the backend. Make sure Kartly.API is running locally.
      </p>
    );
  }

  return (
    <div className="overflow-x-auto rounded-2xl border border-[var(--color-line)] bg-[var(--color-surface)]">
      <table className="w-full text-left text-sm">
        <thead>
          <tr className="border-b border-[var(--color-line)] text-xs uppercase tracking-wide text-[var(--color-ink-soft)]">
            <th className="p-3">Name</th>
            <th className="p-3">Username</th>
            <th className="p-3">Email</th>
            <th className="p-3">Role</th>
            <th className="p-3">Status</th>
            <th className="p-3"></th>
          </tr>
        </thead>
        <tbody>
          {usersQuery.data?.map((u) => (
            <tr key={u.id} className="border-b border-[var(--color-line)] last:border-0">
              <td className="p-3 text-[var(--color-ink)]">{u.firstName} {u.lastName}</td>
              <td className="p-3 font-mono text-xs text-[var(--color-ink-soft)]">@{u.username}</td>
              <td className="p-3 text-[var(--color-ink-soft)]">{u.email}</td>
              <td className="p-3">
                <span className="rounded-full bg-[var(--color-paper)] px-2 py-0.5 text-xs font-600 text-[var(--color-ink)]">
                  {u.role}
                </span>
              </td>
              <td className="p-3">
                <button
                  onClick={() =>
                    toggleActiveMutation.mutate({ id: u.id, role: u.role, isActive: !u.isActive })
                  }
                  className={
                    u.isActive
                      ? "text-xs font-600 text-[var(--color-success)]"
                      : "text-xs font-600 text-[var(--color-danger)]"
                  }
                >
                  {u.isActive ? "Active" : "Deactivated"}
                </button>
              </td>
              <td className="p-3 text-right">
                <button
                  onClick={() => deleteMutation.mutate(u.id)}
                  className="text-xs text-[var(--color-danger)] hover:underline"
                >
                  Delete
                </button>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
