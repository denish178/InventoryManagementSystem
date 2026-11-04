import { Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { DashboardSummary } from '../../core/models';
import { ReportService } from '../../core/report.service';
import { localDate, money, readError } from '../../core/format';

@Component({
  selector: 'app-dashboard',
  imports: [RouterLink],
  templateUrl: './dashboard.component.html',
})
export class DashboardComponent {
  private readonly reports = inject(ReportService);
  readonly loading = signal(true);
  readonly error = signal('');
  readonly summary = signal<DashboardSummary | null>(null);
  readonly localDate = localDate;
  readonly money = money;

  constructor() {
    this.reports.dashboard().subscribe({
      next: (summary) => {
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
