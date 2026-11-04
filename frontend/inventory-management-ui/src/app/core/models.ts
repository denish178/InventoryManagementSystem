export interface Session {
  token: string;
  expiresAt: string;
  email: string;
  fullName: string;
  roles: string[];
}

export interface Category {
  id: number;
  name: string;
  description: string | null;
  isActive: boolean;
  createdAt: string;
}

export interface Supplier {
  id: number;
  name: string;
  contactPerson: string | null;
  email: string | null;
  phone: string | null;
  address: string | null;
  isActive: boolean;
  createdAt: string;
}

export interface Product {
  id: number;
  sku: string;
  name: string;
  description: string | null;
  unitPrice: number;
  quantityInStock: number;
  reorderLevel: number;
  categoryId: number;
  categoryName: string;
  supplierId: number;
  supplierName: string;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface Paged<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
}

export interface StockTransaction {
  id: number;
  productId: number;
  productName: string;
  sku: string;
  transactionType: 'StockIn' | 'StockOut' | string;
  quantity: number;
  referenceNumber: string | null;
  notes: string | null;
  createdAt: string;
  transactionDate: string;
  createdBy: string | null;
}

export interface DashboardSummary {
  totalProducts: number;
  lowStockProducts: number;
  totalUnitsInStock: number;
  inventoryValue: number;
  lowStock: Array<{
    id: number;
    sku: string;
    name: string;
    quantityInStock: number;
    reorderLevel: number;
  }>;
  recentMovements: Array<{
    id: number;
    productId: number;
    productName: string;
    sku: string;
    transactionType: string;
    quantity: number;
    createdAt: string;
  }>;
}

export interface InventoryReport {
  totalProducts: number;
  totalUnits: number;
  inventoryValue: number;
  products: Array<{
    id: number;
    sku: string;
    name: string;
    unitPrice: number;
    quantityInStock: number;
    reorderLevel: number;
    categoryName: string;
    supplierName: string;
    isLowStock: boolean;
    stockValue: number;
  }>;
}

export interface LowStockReport {
  totalLowStockProducts: number;
  products: Array<{
    id: number;
    sku: string;
    name: string;
    unitPrice: number;
    quantityInStock: number;
    reorderLevel: number;
    categoryName: string;
    supplierName: string;
    stockStatus: string;
  }>;
}

export interface MovementReport {
  totalTransactions: number;
  totalStockIn: number;
  totalStockOut: number;
  transactions: StockTransaction[];
}

export interface AppUser {
  id: string;
  fullName: string;
  email: string;
  isActive: boolean;
  createdAt: string;
  roles: string[];
}

export const ROLES = ['Admin', 'Inventory Staff', 'Viewer'] as const;
