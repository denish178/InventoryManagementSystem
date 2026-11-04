import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { Supplier } from './models';

export interface SupplierInput {
  name: string;
  contactPerson: string | null;
  email: string | null;
  phone: string | null;
  address: string | null;
  isActive?: boolean;
}

@Injectable({ providedIn: 'root' })
export class SupplierService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/api/suppliers`;

  list(): Observable<Supplier[]> {
    return this.http.get<Supplier[]>(this.base);
  }

  create(body: SupplierInput): Observable<Supplier> {
    return this.http.post<Supplier>(this.base, body);
  }

  update(id: number, body: SupplierInput): Observable<void> {
    return this.http.put<void>(`${this.base}/${id}`, body);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.base}/${id}`);
  }
}
