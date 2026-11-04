import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { CategoryService } from '../../core/category.service';
import { money, readError } from '../../core/format';
import { Category, InventoryReport, Supplier } from '../../core/models';
import { ReportService } from '../../core/report.service';
import { SupplierService } from '../../core/supplier.service';

@Component({
  selector: 'app-inventory-report',
  imports: [ReactiveFormsModule],
  templateUrl: './inventory-report.component.html',
})
export class InventoryReportComponent {
  private readonly reports = inject(ReportService);
  private readonly categoriesApi = inject(CategoryService);
  private readonly suppliersApi = inject(SupplierService);
  private readonly formBuilder = inject(FormBuilder);

  readonly categories = signal<Category[]>([]);
  readonly suppliers = signal<Supplier[]>([]);
  readonly report = signal<InventoryReport | null>(null);
  readonly loading = signal(true);
  readonly error = signal('');
  readonly money = money;
  readonly filters = this.formBuilder.nonNullable.group({
    search: '',
    categoryId: '',
    supplierId: '',
  });

  constructor() {
    this.categoriesApi.list().subscribe({ next: (items) => this.categories.set(items) });
    this.suppliersApi.list().subscribe({ next: (items) => this.suppliers.set(items) });
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.error.set('');
    const value = this.filters.getRawValue();
    this.reports
      .inventory({
        search: value.search.trim() || undefined,
        categoryId: value.categoryId ? Number(value.categoryId) : null,
        supplierId: value.supplierId ? Number(value.supplierId) : null,
      })
      .subscribe({
        next: (report) => {
          this.report.set(report);
          this.loading.set(false);
        },
        error: (err: unknown) => {
          this.error.set(readError(err));
          this.loading.set(false);
        },
      });
  }
}
