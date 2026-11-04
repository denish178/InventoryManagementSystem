import { Component, inject, signal } from '@angular/core';
import { money, readError } from '../../core/format';
import { LowStockReport } from '../../core/models';
import { ReportService } from '../../core/report.service';

@Component({
  selector: 'app-low-stock-report',
  templateUrl: './low-stock-report.component.html',
})
export class LowStockReportComponent {
  private readonly reports = inject(ReportService);
  readonly report = signal<LowStockReport | null>(null);
  readonly loading = signal(true);
  readonly error = signal('');
  readonly money = money;

  constructor() {
    this.reports.lowStock().subscribe({
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
