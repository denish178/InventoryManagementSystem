import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { EMPTY, Observable, expand, reduce } from 'rxjs';
import { environment } from '../../environments/environment';
import { Paged, Product } from './models';

export interface ProductQuery {
  search?: string;
  categoryId?: number | null;
  supplierId?: number | null;
  lowStock?: boolean;
  page?: number;
  pageSize?: number;
  sort?: string;
  direction?: string;
}

export interface ProductInput {
  sku?: string;
  name: string;
  description: string | null;
  unitPrice: number;
  reorderLevel: number;
  categoryId: number;
  supplierId: number;
  isActive?: boolean;
}

@Injectable({ providedIn: 'root' })
export class ProductService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/api/products`;

  list(query: ProductQuery = {}): Observable<Paged<Product>> {
    let params = new HttpParams()
      .set('page', query.page ?? 1)
      .set('pageSize', query.pageSize ?? 10);
    if (query.search) params = params.set('search', query.search);
    if (query.categoryId) params = params.set('categoryId', query.categoryId);
    if (query.supplierId) params = params.set('supplierId', query.supplierId);
    if (query.lowStock) params = params.set('lowStock', true);
    if (query.sort) params = params.set('sort', query.sort);
    if (query.direction) params = params.set('direction', query.direction);
    return this.http.get<Paged<Product>>(this.base, { params });
  }

  listAll(): Observable<Product[]> {
    const pageSize = 100;
    return this.list({ page: 1, pageSize, sort: 'name', direction: 'asc' }).pipe(
      expand((page) => {
        const loaded = page.page * page.pageSize;
        if (loaded >= page.totalCount) return EMPTY;
        return this.list({ page: page.page + 1, pageSize, sort: 'name', direction: 'asc' });
      }),
      reduce((all, page) => all.concat(page.items), [] as Product[]),
    );
  }

  get(id: number): Observable<Product> {
    return this.http.get<Product>(`${this.base}/${id}`);
  }

  create(body: ProductInput): Observable<Product> {
    return this.http.post<Product>(this.base, body);
  }

  update(id: number, body: ProductInput): Observable<void> {
    return this.http.put<void>(`${this.base}/${id}`, body);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.base}/${id}`);
  }
}
