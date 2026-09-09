import { apiClient } from "./client";
import { authClient } from "./authClient";
import type { AdminProduct, Product, ProductListResponse } from "../types";

// Merges TWO sources into one catalog: DummyJSON (via apiClient) and
// your own Kartly.API products (via authClient, hitting the SAME public
// GET endpoints the Admin Dashboard writes to — [AllowAnonymous] on
// ProductsController). A product the admin just created shows up here
// immediately because it's the exact same database row.

function mapDummyProduct(p: any): Product {
  return {
    key: `dummy-${p.id}`,
    id: String(p.id),
    source: "dummy",
    title: p.title,
    description: p.description,
    price: p.price,
    discountPercentage: p.discountPercentage,
    rating: p.rating,
    stock: p.stock,
    brand: p.brand,
    category: p.category,
    thumbnail: p.thumbnail,
    images: p.images ?? [p.thumbnail],
  };
}

function mapRealProduct(p: AdminProduct): Product {
  return {
    key: `real-${p.id}`,
    id: p.id,
    source: "real",
    title: p.title,
    description: p.description,
    price: p.price,
    discountPercentage: p.discountPercentage,
    rating: p.rating,
    stock: p.stock,
    brand: p.brand,
    category: p.category,
    thumbnail: p.thumbnailUrl,
    images: [p.thumbnailUrl],
  };
}

async function getRealProducts(query?: string, category?: string): Promise<Product[]> {
  try {
    const params = new URLSearchParams();
    if (query) params.set("query", query);
    if (category && category !== "all") params.set("category", category);
    const { data } = await authClient.get(`/products?${params.toString()}`);
    return (data as AdminProduct[]).map(mapRealProduct);
  } catch {
    return []; // backend not running — catalog still works with dummy items
  }
}

export async function getProducts(limit = 20, skip = 0): Promise<ProductListResponse> {
  const [{ data: dummyData }, realProducts] = await Promise.all([
    apiClient.get(`/products?limit=${limit}&skip=${skip}`),
    skip === 0 ? getRealProducts() : Promise.resolve([]),
  ]);
  const dummyProducts = (dummyData.products as any[]).map(mapDummyProduct);
  return { products: [...realProducts, ...dummyProducts], total: dummyData.total + realProducts.length };
}

export async function searchProducts(query: string): Promise<ProductListResponse> {
  const [{ data: dummyData }, realProducts] = await Promise.all([
    apiClient.get(`/products/search?q=${encodeURIComponent(query)}`),
    getRealProducts(query),
  ]);
  const dummyProducts = (dummyData.products as any[]).map(mapDummyProduct);
  const products = [...realProducts, ...dummyProducts];
  return { products, total: products.length };
}

export async function getProductsByCategory(category: string): Promise<ProductListResponse> {
  const [{ data: dummyData }, realProducts] = await Promise.all([
    apiClient.get(`/products/category/${encodeURIComponent(category)}`),
    getRealProducts(undefined, category),
  ]);
  const dummyProducts = (dummyData.products as any[]).map(mapDummyProduct);
  const products = [...realProducts, ...dummyProducts];
  return { products, total: products.length };
}

export async function getCategories(): Promise<string[]> {
  const { data } = await apiClient.get("/products/categories");
  return (data as Array<{ slug: string; name: string }>).map((c) => c.slug);
}

export async function getProductByKey(key: string): Promise<Product> {
  if (key.startsWith("real-")) {
    const { data } = await authClient.get(`/products/${key.replace("real-", "")}`);
    return mapRealProduct(data as AdminProduct);
  }
  const { data } = await apiClient.get(`/products/${key.replace("dummy-", "")}`);
  return mapDummyProduct(data);
}
