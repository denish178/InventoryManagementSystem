import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Observable, tap } from 'rxjs';
import { environment } from '../../environments/environment';
import { Session } from './models';

interface LoginResponse {
  token: string;
  expiresAt: string;
  email: string;
  fullName: string;
  roles: string[];
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly storageKey = 'ims.session';
  readonly session = signal<Session | null>(this.read());
  readonly isAuthenticated = computed(() => this.session() !== null);

  login(email: string, password: string): Observable<LoginResponse> {
    return this.http
      .post<LoginResponse>(`${environment.apiUrl}/api/auth/login`, { email, password })
      .pipe(tap((response) => this.persist(response)));
  }

  logout(): void {
    if (!this.session()) return;
    this.http.post(`${environment.apiUrl}/api/auth/logout`, {}).subscribe({
      next: () => this.clearLocal(),
      error: () => this.clearLocal(),
    });
  }

  clearLocal(): void {
    this.clear();
  }

  hasRole(...roles: string[]): boolean {
    const current = this.session()?.roles ?? [];
    return roles.some((role) => current.includes(role));
  }

  canManageMasterData(): boolean {
    return this.hasRole('Admin');
  }

  canMoveStock(): boolean {
    return this.hasRole('Admin', 'Inventory Staff');
  }

  private persist(response: LoginResponse): void {
    const session: Session = {
      token: response.token,
      expiresAt: response.expiresAt,
      email: response.email,
      fullName: response.fullName,
      roles: response.roles ?? [],
    };
    localStorage.setItem(this.storageKey, JSON.stringify(session));
    this.session.set(session);
  }

  private clear(): void {
    localStorage.removeItem(this.storageKey);
    this.session.set(null);
  }

  private read(): Session | null {
    const raw = localStorage.getItem(this.storageKey);
    if (!raw) return null;
    try {
      const session = JSON.parse(raw) as Session;
      if (!session.token || new Date(session.expiresAt).getTime() <= Date.now()) {
        localStorage.removeItem(this.storageKey);
        return null;
      }
      return session;
    } catch {
      localStorage.removeItem(this.storageKey);
      return null;
    }
  }
}
