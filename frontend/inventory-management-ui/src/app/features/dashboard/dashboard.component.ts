import { Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../core/auth.service';
import { DashboardSummary } from '../../core/models';
import { ReportService } from '../../core/report.service';
import { localDate, money, readError } from '../../core/format';
import { forkJoin } from 'rxjs';

@Component({
  selector: 'app-dashboard',
  imports: [RouterLink],
  templateUrl: './dashboard.component.html',
})
export class DashboardComponent {
  readonly auth = inject(AuthService);
  private readonly reports = inject(ReportService);
  readonly loading = signal(true);
  readonly error = signal('');
  readonly summary = signal<DashboardSummary | null>(null);
  readonly categoryStock = signal<Array<{ name: string; quantity: number; color: string }>>([]);
  readonly localDate = localDate;
  readonly money = money;

  get stockInTotal(): number {
    return this.summary()?.recentMovements
      .filter((item) => item.transactionType === 'StockIn')
      .reduce((total, item) => total + item.quantity, 0) ?? 0;
  }

  get stockOutTotal(): number {
    return this.summary()?.recentMovements
      .filter((item) => item.transactionType === 'StockOut')
      .reduce((total, item) => total + item.quantity, 0) ?? 0;
  }

  get healthyProductCount(): number {
    const data = this.summary();
    return data ? Math.max(0, data.totalProducts - data.lowStockProducts) : 0;
  }

  get categoryStockTotal(): number {
    return this.categoryStock().reduce((total, item) => total + item.quantity, 0);
  }

  get categoryPieBackground(): string {
    const categories = this.categoryStock();
    const total = this.categoryStockTotal;
    if (!categories.length || total <= 0) return '#eef2f6';

    let start = 0;
    const segments = categories.map((item) => {
      const end = start + (item.quantity / total) * 100;
      const segment = `${item.color} ${start}% ${end}%`;
      start = end;
      return segment;
    });
    return `conic-gradient(${segments.join(', ')})`;
  }

  constructor() {
    forkJoin({
      summary: this.reports.dashboard(),
      inventory: this.reports.inventory({}),
    }).subscribe({
      next: ({ summary, inventory }) => {
        const totals = new Map<string, number>();
        for (const product of inventory.products) {
          totals.set(product.categoryName, (totals.get(product.categoryName) ?? 0) + product.quantityInStock);
        }

        const colors = ['#2f7d5b', '#d98b43', '#4775a8', '#b85c72', '#7d6ca4', '#5a8f8f'];
        this.categoryStock.set([...totals.entries()]
          .sort((a, b) => b[1] - a[1])
          .map(([name, quantity], index) => ({ name, quantity, color: colors[index % colors.length] })));
        this.summary.set(summary);
        this.loading.set(false);
      },
      error: (err: unknown) => {
        this.error.set(readError(err));
        this.loading.set(false);
      },
    });
  }
}
