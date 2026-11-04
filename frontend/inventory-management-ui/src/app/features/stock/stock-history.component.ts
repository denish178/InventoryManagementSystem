import { Component, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { localDate, readError } from '../../core/format';
import { Product, StockTransaction } from '../../core/models';
import { ProductService } from '../../core/product.service';
import { StockService } from '../../core/stock.service';

@Component({
  selector: 'app-stock-history',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './stock-history.component.html',
})
export class StockHistoryComponent {
  private readonly stock = inject(StockService);
  private readonly productsApi = inject(ProductService);
  private readonly formBuilder = inject(FormBuilder);

  readonly products = signal<Product[]>([]);
  readonly rows = signal<StockTransaction[]>([]);
  readonly loading = signal(true);
  readonly error = signal('');
  readonly page = signal(1);
  readonly pageSize = 15;
  readonly localDate = localDate;
  readonly filters = this.formBuilder.nonNullable.group({
    productId: '',
    transactionType: '',
    startDate: '',
    endDate: '',
  });

  readonly pageCount = computed(() => Math.max(1, Math.ceil(this.rows().length / this.pageSize)));
  readonly visible = computed(() => {
    const start = (this.page() - 1) * this.pageSize;
    return this.rows().slice(start, start + this.pageSize);
  });

  constructor() {
    this.productsApi.listAll().subscribe({
      next: (items) => this.products.set(items),
      error: () => undefined,
    });
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.error.set('');
    const value = this.filters.getRawValue();
    this.stock
      .list({
        productId: value.productId ? Number(value.productId) : null,
        transactionType: value.transactionType || undefined,
        startDate: value.startDate || undefined,
        endDate: value.endDate || undefined,
      })
      .subscribe({
        next: (rows) => {
          this.rows.set(rows);
          this.page.set(1);
          this.loading.set(false);
        },
        error: (err: unknown) => {
          this.error.set(readError(err));
          this.loading.set(false);
        },
      });
  }

  previous(): void {
    this.page.update((value) => Math.max(1, value - 1));
  }

  next(): void {
    this.page.update((value) => Math.min(this.pageCount(), value + 1));
  }
}
