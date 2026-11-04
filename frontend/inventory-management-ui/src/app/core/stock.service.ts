import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { StockTransaction } from './models';

export interface StockQuery {
  productId?: number | null;
  transactionType?: string;
  startDate?: string;
  endDate?: string;
}

export interface StockMove {
  productId: number;
  quantity: number;
  referenceNumber: string | null;
  notes: string | null;
}

@Injectable({ providedIn: 'root' })
export class StockService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/api/stock-transactions`;

  list(query: StockQuery = {}): Observable<StockTransaction[]> {
    let params = new HttpParams();
    if (query.productId) params = params.set('productId', query.productId);
    if (query.transactionType) params = params.set('transactionType', query.transactionType);
    if (query.startDate) params = params.set('startDate', query.startDate);
    if (query.endDate) params = params.set('endDate', query.endDate);
    return this.http.get<StockTransaction[]>(this.base, { params });
  }

  stockIn(body: StockMove): Observable<StockTransaction> {
    return this.http.post<StockTransaction>(`${this.base}/stock-in`, body);
  }

  stockOut(body: StockMove): Observable<StockTransaction> {
    return this.http.post<StockTransaction>(`${this.base}/stock-out`, body);
  }
}
