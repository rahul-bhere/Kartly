import { useState, type FormEvent } from "react";
import { extractErrorMessage } from "../utils/errorMessage";
import { useAuth } from "../context/AuthContext";
import { validate, isRequired, isValidEmail } from "../utils/validation";
import type { FieldErrors } from "../utils/validation";

export function ProfilePage() {
  const { user, updateProfile, changePassword } = useAuth();
  const [form, setForm] = useState({ firstName: user?.firstName ?? "", lastName: user?.lastName ?? "", email: user?.email ?? "" });
  const [errors, setErrors] = useState<FieldErrors>({});
  const [isSaving, setIsSaving] = useState(false);
  const [saved, setSaved] = useState(false);
  const [serverError, setServerError] = useState<string | null>(null);

  const [pwForm, setPwForm] = useState({ currentPassword: "", newPassword: "", confirmPassword: "" });
  const [pwErrors, setPwErrors] = useState<FieldErrors>({});
  const [isSavingPw, setIsSavingPw] = useState(false);
  const [pwSaved, setPwSaved] = useState(false);
  const [pwServerError, setPwServerError] = useState<string | null>(null);

  if (!user) return null;

  function update<K extends keyof typeof form>(key: K, value: string) {
    setForm((f) => ({ ...f, [key]: value }));
  }

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setServerError(null);
    setSaved(false);
    const fieldErrors = validate({
      firstName: () => isRequired(form.firstName) || "First name is required",
      lastName: () => isRequired(form.lastName) || "Last name is required",
      email: () => isValidEmail(form.email) || "Enter a valid email address",
    });
    setErrors(fieldErrors);
    if (Object.keys(fieldErrors).length > 0) return;
    setIsSaving(true);
    try {
      await updateProfile(form);
      setSaved(true);
      setTimeout(() => setSaved(false), 2500);
    } catch {
      setServerError("Couldn't save your changes. Please try again.");
    } finally {
      setIsSaving(false);
    }
  }

  async function handlePasswordSubmit(e: FormEvent) {
    e.preventDefault();
    setPwServerError(null);
    setPwSaved(false);
    const fieldErrors = validate({
      currentPassword: () => isRequired(pwForm.currentPassword) || "Enter your current password",
      newPassword: () => pwForm.newPassword.length >= 8 || "New password must be at least 8 characters",
      confirmPassword: () => pwForm.newPassword === pwForm.confirmPassword || "Passwords don't match",
    });
    setPwErrors(fieldErrors);
    if (Object.keys(fieldErrors).length > 0) return;
    setIsSavingPw(true);
    try {
      await changePassword({ currentPassword: pwForm.currentPassword, newPassword: pwForm.newPassword });
      setPwSaved(true);
      setPwForm({ currentPassword: "", newPassword: "", confirmPassword: "" });
      setTimeout(() => setPwSaved(false), 2500);
    } catch (err: any) {
      setPwServerError(extractErrorMessage(err, "Couldn't change your password. Check your current password and try again."));
    } finally {
      setIsSavingPw(false);
    }
  }

  return (
    <div className="mx-auto max-w-lg px-6 py-10">
      <h1 className="text-3xl font-700 text-[var(--color-ink)]" style={{ fontFamily: "var(--font-display)" }}>Your profile</h1>
      <p className="mt-1 text-sm text-[var(--color-ink-soft)]">Update your account details below.</p>

      <div className="mt-6 flex items-center gap-4 rounded-2xl border border-[var(--color-line)] bg-[var(--color-surface)] p-4">
        <img src={user.image ?? `https://api.dicebear.com/9.x/initials/svg?seed=${encodeURIComponent(user.username)}`} alt={user.username} className="h-14 w-14 rounded-full object-cover" />
        <div>
          <p className="font-600 text-[var(--color-ink)]">@{user.username} {user.role === "Admin" && <span className="ml-1 rounded-full bg-[var(--color-accent)] px-2 py-0.5 text-[10px] font-600 text-[#1c1c1e]">Admin</span>}</p>
          <p className="text-sm text-[var(--color-ink-soft)]">User ID: {user.id}</p>
        </div>
      </div>

      <form onSubmit={handleSubmit} className="mt-6 flex flex-col gap-4">
        <h2 className="font-600 text-[var(--color-ink)]">Account details</h2>
        <div className="flex gap-3">
          <label className="flex flex-1 flex-col gap-1.5 text-sm">
            <span className="font-medium text-[var(--color-ink)]">First name</span>
            <input value={form.firstName} onChange={(e) => update("firstName", e.target.value)} className="rounded-lg border border-[var(--color-line)] bg-[var(--color-surface)] px-3 py-2.5 outline-none focus:border-[var(--color-primary)]" />
            {errors.firstName && <span className="text-xs text-[var(--color-danger)]">{errors.firstName}</span>}
          </label>
          <label className="flex flex-1 flex-col gap-1.5 text-sm">
            <span className="font-medium text-[var(--color-ink)]">Last name</span>
            <input value={form.lastName} onChange={(e) => update("lastName", e.target.value)} className="rounded-lg border border-[var(--color-line)] bg-[var(--color-surface)] px-3 py-2.5 outline-none focus:border-[var(--color-primary)]" />
            {errors.lastName && <span className="text-xs text-[var(--color-danger)]">{errors.lastName}</span>}
          </label>
        </div>
        <label className="flex flex-col gap-1.5 text-sm">
          <span className="font-medium text-[var(--color-ink)]">Email</span>
          <input value={form.email} onChange={(e) => update("email", e.target.value)} className="rounded-lg border border-[var(--color-line)] bg-[var(--color-surface)] px-3 py-2.5 outline-none focus:border-[var(--color-primary)]" />
          {errors.email && <span className="text-xs text-[var(--color-danger)]">{errors.email}</span>}
        </label>
        {serverError && <p className="text-sm text-[var(--color-danger)]">{serverError}</p>}
        {saved && <p className="text-sm text-[var(--color-success)]">Profile updated ✓</p>}
        <button type="submit" disabled={isSaving} className="mt-2 rounded-full bg-[var(--color-ink)] py-2.5 text-sm font-600 text-[var(--color-paper)] transition hover:bg-[var(--color-primary)] disabled:opacity-60">{isSaving ? "Saving..." : "Save changes"}</button>
      </form>

      <form onSubmit={handlePasswordSubmit} className="mt-10 flex flex-col gap-4 border-t border-[var(--color-line)] pt-8">
        <h2 className="font-600 text-[var(--color-ink)]">Change password</h2>
        <label className="flex flex-col gap-1.5 text-sm">
          <span className="font-medium text-[var(--color-ink)]">Current password</span>
          <input type="password" value={pwForm.currentPassword} onChange={(e) => setPwForm((f) => ({ ...f, currentPassword: e.target.value }))} className="rounded-lg border border-[var(--color-line)] bg-[var(--color-surface)] px-3 py-2.5 outline-none focus:border-[var(--color-primary)]" />
          {pwErrors.currentPassword && <span className="text-xs text-[var(--color-danger)]">{pwErrors.currentPassword}</span>}
        </label>
        <label className="flex flex-col gap-1.5 text-sm">
          <span className="font-medium text-[var(--color-ink)]">New password</span>
          <input type="password" value={pwForm.newPassword} onChange={(e) => setPwForm((f) => ({ ...f, newPassword: e.target.value }))} className="rounded-lg border border-[var(--color-line)] bg-[var(--color-surface)] px-3 py-2.5 outline-none focus:border-[var(--color-primary)]" />
          <span className="text-xs text-[var(--color-ink-soft)]">At least 8 characters, with one uppercase letter, one lowercase letter, and one number.</span>
          {pwErrors.newPassword && <span className="text-xs text-[var(--color-danger)]">{pwErrors.newPassword}</span>}
        </label>
        <label className="flex flex-col gap-1.5 text-sm">
          <span className="font-medium text-[var(--color-ink)]">Confirm new password</span>
          <input type="password" value={pwForm.confirmPassword} onChange={(e) => setPwForm((f) => ({ ...f, confirmPassword: e.target.value }))} className="rounded-lg border border-[var(--color-line)] bg-[var(--color-surface)] px-3 py-2.5 outline-none focus:border-[var(--color-primary)]" />
          {pwErrors.confirmPassword && <span className="text-xs text-[var(--color-danger)]">{pwErrors.confirmPassword}</span>}
        </label>
        {pwServerError && <p className="text-sm text-[var(--color-danger)]">{pwServerError}</p>}
        {pwSaved && <p className="text-sm text-[var(--color-success)]">Password changed ✓</p>}
        <button type="submit" disabled={isSavingPw} className="rounded-full border border-[var(--color-line)] py-2.5 text-sm font-600 text-[var(--color-ink)] transition hover:border-[var(--color-primary)] disabled:opacity-60">{isSavingPw ? "Updating..." : "Change password"}</button>
      </form>
    </div>
  );
}
