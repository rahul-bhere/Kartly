export type ProductSource = "dummy" | "real";

export interface Product {
  key: string;
  id: string;
  source: ProductSource;
  title: string;
  description: string;
  price: number;
  discountPercentage: number;
  rating: number;
  stock: number;
  brand: string;
  category: string;
  thumbnail: string;
  images: string[];
}

export interface ProductListResponse {
  products: Product[];
  total: number;
}

export interface User {
  id: string;
  username: string;
  email: string;
  firstName: string;
  lastName: string;
  role: "User" | "Admin";
  image?: string;
}

export interface AuthUser extends User {
  accessToken: string;
}

export interface LoginPayload {
  username: string;
  password: string;
}

export interface RegisterPayload {
  username: string;
  email: string;
  password: string;
  firstName: string;
  lastName: string;
}

export interface ChangePasswordPayload {
  currentPassword: string;
  newPassword: string;
}

export interface CartItem {
  id: string;
  productId: string | null;
  externalRef: string | null;
  productTitle: string;
  thumbnailUrl: string | null;
  unitPrice: number;
  quantity: number;
  lineTotal: number;
}

export interface Cart {
  id: string;
  items: CartItem[];
  totalAmount: number;
  totalItems: number;
}

export type OrderStatus = "Pending" | "Paid" | "Shipped" | "Delivered" | "Cancelled";
export type PaymentMethod = "CreditCard" | "PayPal" | "CashOnDelivery";

export interface OrderItem {
  productId: string | null;
  externalRef: string | null;
  productTitle: string;
  unitPrice: number;
  quantity: number;
  lineTotal: number;
}

export interface Order {
  id: string;
  userId: string;
  customerName: string;
  customerUsername: string;
  totalAmount: number;
  status: OrderStatus;
  customStatusLabel: string | null;
  shippingAddress: string;
  createdAt: string;
  updatedAt: string | null;
  items: OrderItem[];
}

export interface AdminProduct {
  id: string;
  title: string;
  description: string;
  price: number;
  discountPercentage: number;
  stock: number;
  category: string;
  brand: string;
  thumbnailUrl: string;
  rating: number;
  createdAt: string;
}

export interface AdminProductPayload {
  title: string;
  description: string;
  price: number;
  discountPercentage: number;
  stock: number;
  category: string;
  brand: string;
  thumbnailUrl: string;
}

export interface AdminUser {
  id: string;
  firstName: string;
  lastName: string;
  username: string;
  email: string;
  role: "User" | "Admin";
  isActive: boolean;
  createdAt: string;
}

export interface ChatMessage {
  role: "user" | "assistant";
  content: string;
}

export interface ChatResponse {
  reply: string;
  actionsPerformed: string[];
}
