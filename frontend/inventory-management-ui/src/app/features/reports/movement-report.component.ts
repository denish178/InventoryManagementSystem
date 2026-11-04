import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { localDate, readError } from '../../core/format';
import { MovementReport, Product } from '../../core/models';
import { ProductService } from '../../core/product.service';
import { ReportService } from '../../core/report.service';

@Component({
  selector: 'app-movement-report',
  imports: [ReactiveFormsModule],
  templateUrl: './movement-report.component.html',
})
export class MovementReportComponent {
  private readonly reports = inject(ReportService);
  private readonly productsApi = inject(ProductService);
  private readonly formBuilder = inject(FormBuilder);

  readonly products = signal<Product[]>([]);
  readonly report = signal<MovementReport | null>(null);
  readonly loading = signal(true);
  readonly error = signal('');
  readonly localDate = localDate;
  readonly filters = this.formBuilder.nonNullable.group({
    productId: '',
    transactionType: '',
    startDate: '',
    endDate: '',
  });

  constructor() {
    this.productsApi.listAll().subscribe({ next: (items) => this.products.set(items) });
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.error.set('');
    const value = this.filters.getRawValue();
    this.reports
      .movements({
        productId: value.productId ? Number(value.productId) : null,
        transactionType: value.transactionType || undefined,
        startDate: value.startDate || undefined,
        endDate: value.endDate || undefined,
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
