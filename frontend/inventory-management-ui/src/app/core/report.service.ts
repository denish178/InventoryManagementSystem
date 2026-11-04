import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { DashboardSummary, InventoryReport, LowStockReport, MovementReport } from './models';

@Injectable({ providedIn: 'root' })
export class ReportService {
  private readonly http = inject(HttpClient);

  dashboard(): Observable<DashboardSummary> {
    return this.http.get<DashboardSummary>(`${environment.apiUrl}/api/dashboard/summary`);
  }

  inventory(query: { search?: string; categoryId?: number | null; supplierId?: number | null }): Observable<InventoryReport> {
    let params = new HttpParams();
    if (query.search) params = params.set('search', query.search);
    if (query.categoryId) params = params.set('categoryId', query.categoryId);
    if (query.supplierId) params = params.set('supplierId', query.supplierId);
    return this.http.get<InventoryReport>(`${environment.apiUrl}/api/reports/inventory`, { params });
  }

  lowStock(): Observable<LowStockReport> {
    return this.http.get<LowStockReport>(`${environment.apiUrl}/api/reports/low-stock`);
  }

  movements(query: {
    productId?: number | null;
    transactionType?: string;
    startDate?: string;
    endDate?: string;
  }): Observable<MovementReport> {
    let params = new HttpParams();
    if (query.productId) params = params.set('productId', query.productId);
    if (query.transactionType) params = params.set('transactionType', query.transactionType);
    if (query.startDate) params = params.set('startDate', query.startDate);
    if (query.endDate) params = params.set('endDate', query.endDate);
    return this.http.get<MovementReport>(`${environment.apiUrl}/api/reports/stock-movements`, { params });
  }
}
