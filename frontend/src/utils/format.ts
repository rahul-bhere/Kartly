export function formatPrice(value: number): string {
  return new Intl.NumberFormat("en-US", {
    style: "currency",
    currency: "USD",
  }).format(value);
}

export function discountedPrice(price: number, discountPercentage: number): number {
  return price - (price * discountPercentage) / 100;
}
