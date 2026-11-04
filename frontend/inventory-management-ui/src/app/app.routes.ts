import { Routes } from '@angular/router';
import { authGuard, guestGuard, roleGuard } from './core/auth.guard';

export const routes: Routes = [
  {
    path: 'login',
    canActivate: [guestGuard],
    loadComponent: () => import('./features/auth/login.component').then((m) => m.LoginComponent),
  },
  {
    path: '',
    canActivate: [authGuard],
    loadComponent: () => import('./features/shell/shell.component').then((m) => m.ShellComponent),
    children: [
      {
        path: '',
        loadComponent: () => import('./features/dashboard/dashboard.component').then((m) => m.DashboardComponent),
      },
      {
        path: 'products',
        loadComponent: () => import('./features/products/products.component').then((m) => m.ProductsComponent),
      },
      {
        path: 'products/new',
        canActivate: [roleGuard('Admin')],
        loadComponent: () => import('./features/products/product-form.component').then((m) => m.ProductFormComponent),
      },
      {
        path: 'products/:id',
        loadComponent: () => import('./features/products/product-detail.component').then((m) => m.ProductDetailComponent),
      },
      {
        path: 'products/:id/edit',
        canActivate: [roleGuard('Admin')],
        loadComponent: () => import('./features/products/product-form.component').then((m) => m.ProductFormComponent),
      },
      {
        path: 'categories',
        loadComponent: () => import('./features/categories/categories.component').then((m) => m.CategoriesComponent),
      },
      {
        path: 'suppliers',
        loadComponent: () => import('./features/suppliers/suppliers.component').then((m) => m.SuppliersComponent),
      },
      {
        path: 'stock',
        pathMatch: 'full',
        redirectTo: 'stock/history',
      },
      {
        path: 'stock/in',
        canActivate: [roleGuard('Admin', 'Inventory Staff')],
        data: { mode: 'in' },
        loadComponent: () => import('./features/stock/stock-move.component').then((m) => m.StockMoveComponent),
      },
      {
        path: 'stock/out',
        canActivate: [roleGuard('Admin', 'Inventory Staff')],
        data: { mode: 'out' },
        loadComponent: () => import('./features/stock/stock-move.component').then((m) => m.StockMoveComponent),
      },
      {
        path: 'stock/history',
        loadComponent: () => import('./features/stock/stock-history.component').then((m) => m.StockHistoryComponent),
      },
      {
        path: 'reports',
        pathMatch: 'full',
        redirectTo: 'reports/inventory',
      },
      {
        path: 'reports/inventory',
        loadComponent: () => import('./features/reports/inventory-report.component').then((m) => m.InventoryReportComponent),
      },
      {
        path: 'reports/low-stock',
        loadComponent: () => import('./features/reports/low-stock-report.component').then((m) => m.LowStockReportComponent),
      },
      {
        path: 'reports/movements',
        loadComponent: () => import('./features/reports/movement-report.component').then((m) => m.MovementReportComponent),
      },
      {
        path: 'users',
        canActivate: [roleGuard('Admin')],
        loadComponent: () => import('./features/users/users.component').then((m) => m.UsersComponent),
      },
    ],
  },
  { path: '**', redirectTo: '' },
];
