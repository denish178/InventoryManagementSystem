import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { readError } from '../../core/format';
import { AppUser, ROLES } from '../../core/models';
import { NoticeService } from '../../core/notice.service';
import { UserService } from '../../core/user.service';

@Component({
  selector: 'app-users',
  imports: [ReactiveFormsModule],
  templateUrl: './users.component.html',
})
export class UsersComponent {
  private readonly usersApi = inject(UserService);
  private readonly formBuilder = inject(FormBuilder);
  private readonly notice = inject(NoticeService);

  readonly roles = ROLES;
  readonly users = signal<AppUser[]>([]);
  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly error = signal('');
  readonly form = this.formBuilder.nonNullable.group({
    fullName: ['', [Validators.required, Validators.maxLength(100)]],
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(8)]],
    role: ['Viewer', Validators.required],
  });

  constructor() {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.usersApi.list().subscribe({
      next: (users) => {
        this.users.set(users);
        this.loading.set(false);
      },
      error: (err: unknown) => {
        this.error.set(readError(err));
        this.loading.set(false);
      },
    });
  }

  create(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    const value = this.form.getRawValue();
    this.saving.set(true);
    this.error.set('');
    this.usersApi
      .create({
        fullName: value.fullName.trim(),
        email: value.email.trim(),
        password: value.password,
        role: value.role,
      })
      .subscribe({
        next: () => {
          this.saving.set(false);
          this.notice.show('User created.');
          this.form.reset({ fullName: '', email: '', password: '', role: 'Viewer' });
          this.load();
        },
        error: (err: unknown) => {
          this.saving.set(false);
          this.error.set(readError(err));
        },
      });
  }

  changeRole(user: AppUser, role: string): void {
    if (!role || user.roles.includes(role)) return;
    this.error.set('');
    this.usersApi.changeRole(user.id, role).subscribe({
      next: () => {
        this.notice.show(`Role updated for ${user.email}.`);
        this.load();
      },
      error: (err: unknown) => this.error.set(readError(err)),
    });
  }
}
