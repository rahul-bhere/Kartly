import { useMemo, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import * as productApi from "../api/productApi";
import { ProductCard } from "../components/ProductCard";
import { Loader } from "../components/Loader";
import { EmptyState } from "../components/EmptyState";
import { useDebounce } from "../hooks";

const PAGE_SIZE = 12;

export function ProductsPage() {
  const [search, setSearch] = useState("");
  const debouncedSearch = useDebounce(search, 350);
  const [category, setCategory] = useState<string>("all");
  const [page, setPage] = useState(0);

  const { data: categories } = useQuery({ queryKey: ["categories"], queryFn: productApi.getCategories });

  const productsQuery = useQuery({
    queryKey: ["products", debouncedSearch, category, page],
    queryFn: () => {
      if (debouncedSearch.trim()) return productApi.searchProducts(debouncedSearch.trim());
      if (category !== "all") return productApi.getProductsByCategory(category);
      return productApi.getProducts(PAGE_SIZE, page * PAGE_SIZE);
    },
  });

  const isPaginated = !debouncedSearch.trim() && category === "all";
  const totalPages = useMemo(() => {
    if (!productsQuery.data || !isPaginated) return 1;
    return Math.ceil(productsQuery.data.total / PAGE_SIZE);
  }, [productsQuery.data, isPaginated]);

  return (
    <div className="mx-auto max-w-6xl px-6 py-10">
      <div className="flex flex-col gap-1">
        <h1 className="text-3xl font-700 text-[var(--color-ink)]" style={{ fontFamily: "var(--font-display)" }}>Catalog</h1>
        <p className="text-sm text-[var(--color-ink-soft)]">A mix of the public demo catalog and products added by the store's admin — look for the "Store pick" tag.</p>
      </div>

      <div className="mt-6 flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <input value={search} onChange={(e) => { setSearch(e.target.value); setPage(0); }} placeholder="Search products..." className="w-full rounded-full border border-[var(--color-line)] bg-[var(--color-surface)] px-4 py-2.5 text-sm outline-none focus:border-[var(--color-primary)] sm:max-w-xs" />
        <select value={category} onChange={(e) => { setCategory(e.target.value); setPage(0); }} className="rounded-full border border-[var(--color-line)] bg-[var(--color-surface)] px-4 py-2.5 text-sm outline-none focus:border-[var(--color-primary)]">
          <option value="all">All categories</option>
          {categories?.map((c) => <option key={c} value={c}>{c}</option>)}
        </select>
      </div>

      <div className="mt-8">
        {productsQuery.isLoading && <Loader label="Fetching products" />}
        {productsQuery.isError && <EmptyState title="Couldn't load products" description="Something went wrong reaching the API. Please try again." />}
        {productsQuery.data && productsQuery.data.products.length === 0 && <EmptyState title="No products found" description="Try a different search term or category." />}
        {productsQuery.data && productsQuery.data.products.length > 0 && (
          <div className="grid grid-cols-2 gap-5 sm:grid-cols-3 lg:grid-cols-4">
            {productsQuery.data.products.map((product) => <ProductCard key={product.key} product={product} />)}
          </div>
        )}
      </div>

      {isPaginated && totalPages > 1 && (
        <div className="mt-10 flex items-center justify-center gap-4">
          <button disabled={page === 0} onClick={() => setPage((p) => p - 1)} className="rounded-full border border-[var(--color-line)] px-4 py-2 text-sm disabled:opacity-40">Previous</button>
          <span className="font-mono text-xs text-[var(--color-ink-soft)]">Page {page + 1} of {totalPages}</span>
          <button disabled={page + 1 >= totalPages} onClick={() => setPage((p) => p + 1)} className="rounded-full border border-[var(--color-line)] px-4 py-2 text-sm disabled:opacity-40">Next</button>
        </div>
      )}
    </div>
  );
}
