import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { Category } from './models';

@Injectable({ providedIn: 'root' })
export class CategoryService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/api/categories`;

  list(): Observable<Category[]> {
    return this.http.get<Category[]>(this.base);
  }

  create(body: { name: string; description: string | null }): Observable<Category> {
    return this.http.post<Category>(this.base, body);
  }

  update(id: number, body: { name: string; description: string | null; isActive: boolean }): Observable<void> {
    return this.http.put<void>(`${this.base}/${id}`, body);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.base}/${id}`);
  }
}
