import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../core/auth.service';
import { CategoryService } from '../../core/category.service';
import { ConfirmService } from '../../core/confirm.service';
import { isLowStock, money, readError } from '../../core/format';
import { Category, Product, Supplier } from '../../core/models';
import { NoticeService } from '../../core/notice.service';
import { ProductService } from '../../core/product.service';
import { SupplierService } from '../../core/supplier.service';

@Component({
  selector: 'app-products',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './products.component.html',
})
export class ProductsComponent {
  readonly auth = inject(AuthService);
  private readonly products = inject(ProductService);
  private readonly categoriesApi = inject(CategoryService);
  private readonly suppliersApi = inject(SupplierService);
  private readonly formBuilder = inject(FormBuilder);
  private readonly confirm = inject(ConfirmService);
  private readonly notice = inject(NoticeService);

  readonly items = signal<Product[]>([]);
  readonly categories = signal<Category[]>([]);
  readonly suppliers = signal<Supplier[]>([]);
  readonly total = signal(0);
  readonly page = signal(1);
  readonly pageSize = 10;
  readonly loading = signal(true);
  readonly error = signal('');
  readonly sort = signal('name');
  readonly direction = signal<'asc' | 'desc'>('asc');
  readonly money = money;
  readonly isLowStock = isLowStock;
  readonly filters = this.formBuilder.nonNullable.group({
    search: '',
    categoryId: '',
    supplierId: '',
    lowStock: false,
  });

  constructor() {
    this.categoriesApi.list().subscribe({
      next: (items) => this.categories.set(items),
      error: (err: unknown) => this.error.set(readError(err)),
    });
    this.suppliersApi.list().subscribe({
      next: (items) => this.suppliers.set(items),
      error: (err: unknown) => this.error.set(readError(err)),
    });
    this.load();
  }

  get totalPages(): number {
    return Math.max(1, Math.ceil(this.total() / this.pageSize));
  }

  load(): void {
    this.loading.set(true);
    this.error.set('');
    const value = this.filters.getRawValue();
    this.products
      .list({
        search: value.search.trim() || undefined,
        categoryId: value.categoryId ? Number(value.categoryId) : null,
        supplierId: value.supplierId ? Number(value.supplierId) : null,
        lowStock: value.lowStock,
        page: this.page(),
        pageSize: this.pageSize,
        sort: this.sort(),
        direction: this.direction(),
      })
      .subscribe({
        next: (page) => {
          this.items.set(page.items);
          this.total.set(page.totalCount);
          this.loading.set(false);
        },
        error: (err: unknown) => {
          this.error.set(readError(err));
          this.loading.set(false);
        },
      });
  }

  search(): void {
    this.page.set(1);
    this.load();
  }

  toggleSort(column: string): void {
    if (this.sort() === column) {
      this.direction.set(this.direction() === 'asc' ? 'desc' : 'asc');
    } else {
      this.sort.set(column);
      this.direction.set('asc');
    }
    this.page.set(1);
    this.load();
  }

  previous(): void {
    if (this.page() <= 1) return;
    this.page.update((value) => value - 1);
    this.load();
  }

  next(): void {
    if (this.page() >= this.totalPages) return;
    this.page.update((value) => value + 1);
    this.load();
  }

  async remove(item: Product): Promise<void> {
    const ok = await this.confirm.ask(
      'Delete product',
      `Delete ${item.sku}? Products with stock history cannot be deleted.`,
    );
    if (!ok) return;
    this.products.delete(item.id).subscribe({
      next: () => {
        this.notice.show('Product deleted.');
        this.load();
      },
      error: (err: unknown) => this.error.set(readError(err)),
    });
  }
}
