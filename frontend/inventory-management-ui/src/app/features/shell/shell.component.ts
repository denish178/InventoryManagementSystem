import { Component, computed, inject } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from '../../core/auth.service';

@Component({
  selector: 'app-shell',
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  templateUrl: './shell.component.html',
})
export class ShellComponent {
  readonly auth = inject(AuthService);
  readonly links = computed(() => {
    const roles = this.auth.session()?.roles ?? [];
    const admin = roles.includes('Admin');
    return [
      { path: '/', label: 'Dashboard', exact: true },
      { path: '/products', label: 'Products', exact: false },
      { path: '/categories', label: 'Categories', exact: false },
      { path: '/suppliers', label: 'Suppliers', exact: false },
      { path: '/stock', label: 'Stock', exact: false },
      { path: '/reports', label: 'Reports', exact: false },
      ...(admin ? [{ path: '/users', label: 'Users', exact: false }] : []),
    ];
  });

  logout(): void {
    this.auth.logout();
  }
}
