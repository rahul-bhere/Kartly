import { authClient } from "./authClient";
import type { AdminProduct, AdminProductPayload } from "../types";

// Matches Kartly.API's ProductsController + ProductDto exactly.
export async function getAdminProducts(): Promise<AdminProduct[]> {
  const { data } = await authClient.get("/products");
  return data as AdminProduct[];
}

export async function createAdminProduct(payload: AdminProductPayload): Promise<AdminProduct> {
  const { data } = await authClient.post("/products", payload);
  return data as AdminProduct;
}

export async function updateAdminProduct(id: string, payload: AdminProductPayload): Promise<AdminProduct> {
  const { data } = await authClient.put(`/products/${id}`, payload);
  return data as AdminProduct;
}

export async function deleteAdminProduct(id: string): Promise<void> {
  await authClient.delete(`/products/${id}`);
}

// Uploads an image file, returns the URL to use as thumbnailUrl. The
// backend saves it to wwwroot/uploads and serves it as a static file —
// see ProductsController.UploadImage for validation rules (5MB max,
// JPEG/PNG/WEBP/GIF only).
export async function uploadProductImage(file: File): Promise<string> {
  const formData = new FormData();
  formData.append("file", file);
  const { data } = await authClient.post("/products/upload-image", formData, {
    headers: { "Content-Type": "multipart/form-data" },
  });
  return data.url as string;
}
