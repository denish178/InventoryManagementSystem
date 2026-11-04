import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { AppUser } from './models';

@Injectable({ providedIn: 'root' })
export class UserService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/api/users`;

  list(): Observable<AppUser[]> {
    return this.http.get<AppUser[]>(this.base);
  }

  create(body: { fullName: string; email: string; password: string; role: string }): Observable<AppUser> {
    return this.http.post<AppUser>(this.base, body);
  }

  changeRole(id: string, role: string): Observable<{ message: string }> {
    return this.http.put<{ message: string }>(`${this.base}/${id}/role`, { role });
  }
}
