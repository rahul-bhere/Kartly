import { useState, type ChangeEvent, type ReactNode } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import * as adminProductApi from "../../api/adminProductApi";
import type { AdminProduct, AdminProductPayload } from "../../types";
import { formatPrice } from "../../utils/format";
import { validate, isRequired } from "../../utils/validation";
import type { FieldErrors } from "../../utils/validation";
import { Loader } from "../Loader";

// -----------------------------------------------------------------------
// WHY NUMERIC FIELDS ARE STRINGS HERE, NOT NUMBERS:
// Binding a <input type="number"> directly to a `number` state value
// (and converting with Number(e.target.value) on every keystroke) is a
// classic controlled-input bug: Number("") is 0, so the moment the field
// becomes empty (backspacing the last digit, or selecting all + delete),
// React re-renders the input with value={0} — the "0" comes right back
// and you can never actually clear the field. Keeping these as free-text
// string state during editing (and only parsing to a real number at
// submit time, in toPayload() below) fixes that completely: the field
// can be blank, can be backspaced normally, and still validates properly
// before it's ever sent to the backend.
// -----------------------------------------------------------------------
interface FormState {
  title: string;
  description: string;
  price: string;
  discountPercentage: string;
  stock: string;
  category: string;
  brand: string;
  thumbnailUrl: string;
}

const EMPTY_FORM: FormState = {
  title: "",
  description: "",
  price: "",
  discountPercentage: "",
  stock: "",
  category: "",
  brand: "",
  thumbnailUrl: "",
};

function toPayload(form: FormState): AdminProductPayload {
  return {
    title: form.title.trim(),
    description: form.description.trim(),
    price: form.price === "" ? 0 : Number(form.price),
    discountPercentage: form.discountPercentage === "" ? 0 : Number(form.discountPercentage),
    stock: form.stock === "" ? 0 : Number(form.stock),
    category: form.category.trim(),
    brand: form.brand.trim(),
    thumbnailUrl: form.thumbnailUrl.trim(),
  };
}

function fromProduct(product: AdminProduct): FormState {
  return {
    title: product.title,
    description: product.description,
    price: String(product.price),
    discountPercentage: String(product.discountPercentage),
    stock: String(product.stock),
    category: product.category,
    brand: product.brand,
    thumbnailUrl: product.thumbnailUrl,
  };
}

export function ProductsTab() {
  const queryClient = useQueryClient();
  const [form, setForm] = useState<FormState>(EMPTY_FORM);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [errors, setErrors] = useState<FieldErrors>({});
  const [apiError, setApiError] = useState<string | null>(null);

  const uploadMutation = useMutation({
    mutationFn: adminProductApi.uploadProductImage,
    onSuccess: (url) => setForm((f) => ({ ...f, thumbnailUrl: url })),
    onError: () => setApiError("Couldn't upload that image. Check it's a JPEG/PNG/WEBP/GIF under 5MB."),
  });

  function handleFileSelected(e: ChangeEvent<HTMLInputElement>) {
    const file = e.target.files?.[0];
    if (!file) return;
    setApiError(null);
    uploadMutation.mutate(file);
    e.target.value = ""; // allow re-selecting the same file later
  }

  const productsQuery = useQuery({
    queryKey: ["admin-products"],
    queryFn: adminProductApi.getAdminProducts,
  });

  const createMutation = useMutation({
    mutationFn: adminProductApi.createAdminProduct,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["admin-products"] });
      setForm(EMPTY_FORM);
    },
    onError: () => setApiError("Couldn't create the product. Is the backend running?"),
  });

  const updateMutation = useMutation({
    mutationFn: ({ id, payload }: { id: string; payload: AdminProductPayload }) =>
      adminProductApi.updateAdminProduct(id, payload),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["admin-products"] });
      setEditingId(null);
      setForm(EMPTY_FORM);
    },
    onError: () => setApiError("Couldn't update the product."),
  });

  const deleteMutation = useMutation({
    mutationFn: adminProductApi.deleteAdminProduct,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["admin-products"] }),
    onError: () => setApiError("Couldn't delete the product."),
  });

  function startEdit(product: AdminProduct) {
    setEditingId(product.id);
    setForm(fromProduct(product));
    setErrors({});
  }

  function cancelEdit() {
    setEditingId(null);
    setForm(EMPTY_FORM);
    setErrors({});
  }

  function handleSubmit() {
    setApiError(null);
    const payload = toPayload(form);
    const fieldErrors = validate({
      title: () => isRequired(payload.title) || "Title is required",
      category: () => isRequired(payload.category) || "Category is required",
      brand: () => isRequired(payload.brand) || "Brand is required",
      thumbnailUrl: () => isRequired(payload.thumbnailUrl) || "Thumbnail URL is required",
      price: () => payload.price > 0 || "Price must be greater than 0",
      discountPercentage: () =>
        (payload.discountPercentage >= 0 && payload.discountPercentage <= 100) || "Discount must be between 0 and 100",
      stock: () => payload.stock >= 0 || "Stock cannot be negative",
    });
    setErrors(fieldErrors);
    if (Object.keys(fieldErrors).length > 0) return;

    if (editingId) {
      updateMutation.mutate({ id: editingId, payload });
    } else {
      createMutation.mutate(payload);
    }
  }

  const isSaving = createMutation.isPending || updateMutation.isPending;

  return (
    <div className="grid grid-cols-1 gap-8 lg:grid-cols-[1fr_1.3fr]">
      {/* Create / Edit form */}
      <div className="h-fit rounded-2xl border border-[var(--color-line)] bg-[var(--color-surface)] p-5">
        <h3 className="font-600 text-[var(--color-ink)]">
          {editingId ? "Edit product" : "Create new product"}
        </h3>
        <p className="mt-1 text-xs text-[var(--color-ink-soft)]">
          Saved to your SQL Server database via Kartly.API — separate from the dummy catalog shoppers see.
        </p>

        <div className="mt-4 flex flex-col gap-3">
          <Field label="Title" error={errors.title}>
            <input
              value={form.title}
              onChange={(e) => setForm({ ...form, title: e.target.value })}
              className="input"
            />
          </Field>
          <Field label="Description">
            <textarea
              value={form.description}
              onChange={(e) => setForm({ ...form, description: e.target.value })}
              rows={3}
              className="input"
            />
          </Field>
          <div className="grid grid-cols-2 gap-3">
            <Field label="Price ($)" error={errors.price}>
              <input
                type="number"
                min={0}
                step="0.01"
                placeholder="0.00"
                value={form.price}
                onChange={(e) => setForm({ ...form, price: e.target.value })}
                className="input"
              />
            </Field>
            <Field label="Discount (%)" error={errors.discountPercentage}>
              <input
                type="number"
                min={0}
                max={100}
                placeholder="0"
                value={form.discountPercentage}
                onChange={(e) => setForm({ ...form, discountPercentage: e.target.value })}
                className="input"
              />
            </Field>
          </div>
          <div className="grid grid-cols-2 gap-3">
            <Field label="Stock" error={errors.stock}>
              <input
                type="number"
                min={0}
                placeholder="0"
                value={form.stock}
                onChange={(e) => setForm({ ...form, stock: e.target.value })}
                className="input"
              />
            </Field>
            <Field label="Category" error={errors.category}>
              <input
                value={form.category}
                onChange={(e) => setForm({ ...form, category: e.target.value })}
                className="input"
              />
            </Field>
          </div>
          <Field label="Brand" error={errors.brand}>
            <input
              value={form.brand}
              onChange={(e) => setForm({ ...form, brand: e.target.value })}
              className="input"
            />
          </Field>
          <Field label="Product image" error={errors.thumbnailUrl}>
            <div className="flex items-center gap-3">
              {form.thumbnailUrl ? (
                <img src={form.thumbnailUrl} alt="" className="h-16 w-16 rounded-lg border border-[var(--color-line)] object-cover" />
              ) : (
                <div className="flex h-16 w-16 items-center justify-center rounded-lg border border-dashed border-[var(--color-line)] text-[10px] text-[var(--color-ink-soft)]">No image</div>
              )}
              <div className="flex-1">
                <input type="file" accept="image/jpeg,image/png,image/webp,image/gif" onChange={handleFileSelected} className="input w-full" />
                {uploadMutation.isPending && <p className="mt-1 text-xs text-[var(--color-ink-soft)]">Uploading...</p>}
              </div>
            </div>
            <input
              value={form.thumbnailUrl}
              onChange={(e) => setForm({ ...form, thumbnailUrl: e.target.value })}
              placeholder="...or paste an image URL directly"
              className="input mt-2"
            />
          </Field>

          {apiError && <p className="text-xs text-[var(--color-danger)]">{apiError}</p>}

          <div className="flex gap-2">
            <button
              onClick={handleSubmit}
              disabled={isSaving}
              className="flex-1 rounded-full bg-[var(--color-ink)] py-2.5 text-sm font-600 text-[var(--color-paper)] transition hover:bg-[var(--color-primary)] disabled:opacity-60"
            >
              {isSaving ? "Saving..." : editingId ? "Update product" : "Create product"}
            </button>
            {editingId && (
              <button
                onClick={cancelEdit}
                className="rounded-full border border-[var(--color-line)] px-4 text-sm text-[var(--color-ink)]"
              >
                Cancel
              </button>
            )}
          </div>
        </div>
      </div>

      {/* Product list */}
      <div>
        {productsQuery.isLoading && <Loader label="Loading your products" />}
        {productsQuery.isError && (
          <p className="rounded-lg border border-dashed border-[var(--color-line)] p-4 text-sm text-[var(--color-ink-soft)]">
            Couldn't reach the backend at the configured VITE_ADMIN_API_URL.
            Make sure `dotnet run --project src/Kartly.API` is running.
          </p>
        )}
        {productsQuery.data && productsQuery.data.length === 0 && (
          <p className="rounded-lg border border-dashed border-[var(--color-line)] p-4 text-sm text-[var(--color-ink-soft)]">
            No products yet — create your first one using the form.
          </p>
        )}
        <ul className="flex flex-col gap-3">
          {productsQuery.data?.map((product) => (
            <li
              key={product.id}
              className="flex items-center gap-3 rounded-2xl border border-[var(--color-line)] bg-[var(--color-surface)] p-3"
            >
              <img src={product.thumbnailUrl} alt="" className="h-14 w-14 rounded-lg object-cover" />
              <div className="flex-1">
                <p className="font-600 text-[var(--color-ink)]">{product.title}</p>
                <p className="text-xs text-[var(--color-ink-soft)]">
                  {product.category} · {formatPrice(product.price)} · stock {product.stock}
                </p>
              </div>
              <button
                onClick={() => startEdit(product)}
                className="rounded-full border border-[var(--color-line)] px-3 py-1.5 text-xs text-[var(--color-ink)]"
              >
                Edit
              </button>
              <button
                onClick={() => deleteMutation.mutate(product.id)}
                className="rounded-full border border-[var(--color-line)] px-3 py-1.5 text-xs text-[var(--color-danger)]"
              >
                Delete
              </button>
            </li>
          ))}
        </ul>
      </div>
    </div>
  );
}

function Field({
  label,
  error,
  children,
}: {
  label: string;
  error?: string;
  children: ReactNode;
}) {
  return (
    <label className="flex flex-col gap-1.5 text-sm">
      <span className="font-medium text-[var(--color-ink)]">{label}</span>
      {children}
      {error && <span className="text-xs text-[var(--color-danger)]">{error}</span>}
    </label>
  );
}
