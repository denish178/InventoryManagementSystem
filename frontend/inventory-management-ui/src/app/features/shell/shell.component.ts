import { Component, computed, inject, signal } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from '../../core/auth.service';
import { ConfirmService } from '../../core/confirm.service';
import { NoticeService } from '../../core/notice.service';

interface NavLink {
  path: string;
  label: string;
  exact: boolean;
}

interface NavGroup {
  label: string;
  items: NavLink[];
}

@Component({
  selector: 'app-shell',
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  templateUrl: './shell.component.html',
})
export class ShellComponent {
  readonly auth = inject(AuthService);
  readonly confirm = inject(ConfirmService);
  readonly notice = inject(NoticeService);
  private readonly router = inject(Router);
  readonly menuOpen = signal(false);

  readonly groups = computed<NavGroup[]>(() => {
    const groups: NavGroup[] = [
      { label: 'Overview', items: [{ path: '/', label: 'Dashboard', exact: true }] },
      {
        label: 'Master data',
        items: [
          { path: '/products', label: 'Products', exact: false },
          { path: '/categories', label: 'Categories', exact: false },
          { path: '/suppliers', label: 'Suppliers', exact: false },
        ],
      },
      {
        label: 'Stock',
        items: [
          ...(this.auth.canMoveStock()
            ? [
                { path: '/stock/in', label: 'Stock in', exact: true },
                { path: '/stock/out', label: 'Stock out', exact: true },
              ]
            : []),
          { path: '/stock/history', label: 'History', exact: true },
        ],
      },
      {
        label: 'Reports',
        items: [
          { path: '/reports/inventory', label: 'Inventory', exact: true },
          { path: '/reports/low-stock', label: 'Low stock', exact: true },
          { path: '/reports/movements', label: 'Movements', exact: true },
        ],
      },
    ];

    if (this.auth.hasRole('Admin')) {
      groups.push({ label: 'Administration', items: [{ path: '/users', label: 'Users', exact: false }] });
    }

    return groups;
  });

  closeMenu(): void {
    this.menuOpen.set(false);
  }

  logout(): void {
    this.auth.logout();
    void this.router.navigate(['/login']);
  }
}
