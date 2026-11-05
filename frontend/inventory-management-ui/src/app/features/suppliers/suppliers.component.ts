import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { AuthService } from '../../core/auth.service';
import { ConfirmService } from '../../core/confirm.service';
import { readError } from '../../core/format';
import { Supplier } from '../../core/models';
import { NoticeService } from '../../core/notice.service';
import { SupplierService } from '../../core/supplier.service';
import { map } from 'rxjs';

@Component({
  selector: 'app-suppliers',
  imports: [ReactiveFormsModule],
  templateUrl: './suppliers.component.html',
})
export class SuppliersComponent {
  readonly auth = inject(AuthService);
  private readonly api = inject(SupplierService);
  private readonly confirm = inject(ConfirmService);
  private readonly notice = inject(NoticeService);
  private readonly formBuilder = inject(FormBuilder);

  readonly items = signal<Supplier[]>([]);
  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly error = signal('');
  readonly editingId = signal<number | null>(null);
  readonly form = this.formBuilder.nonNullable.group({
    name: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(100)]],
    contactPerson: ['', Validators.maxLength(100)],
    email: ['', Validators.email],
    phone: ['', [Validators.maxLength(20), Validators.pattern(/^[+0-9][0-9\s().-]{6,19}$/)]],
    address: ['', Validators.maxLength(300)],
    isActive: true,
  });

  constructor() {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.api.list().subscribe({
      next: (items) => {
        this.items.set(items);
        this.loading.set(false);
      },
      error: (err: unknown) => {
        this.error.set(readError(err));
        this.loading.set(false);
      },
    });
  }

  edit(item: Supplier): void {
    this.editingId.set(item.id);
    this.form.setValue({
      name: item.name,
      contactPerson: item.contactPerson ?? '',
      email: item.email ?? '',
      phone: item.phone ?? '',
      address: item.address ?? '',
      isActive: item.isActive,
    });
  }

  cancel(): void {
    this.editingId.set(null);
    this.form.reset({ name: '', contactPerson: '', email: '', phone: '', address: '', isActive: true });
  }

  save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    const value = this.form.getRawValue();
    const body = {
      name: value.name.trim(),
      contactPerson: value.contactPerson.trim() || null,
      email: value.email.trim() || null,
      phone: value.phone.trim() || null,
      address: value.address.trim() || null,
      isActive: value.isActive,
    };
    this.saving.set(true);
    this.error.set('');
    const id = this.editingId();

const request = id
  ? this.api.update(id, body).pipe(map(() => undefined))
  : this.api.create(body).pipe(map(() => undefined));
    request.subscribe({
      next: () => {
        this.saving.set(false);
        this.notice.show(this.editingId() ? 'Supplier updated.' : 'Supplier created.');
        this.cancel();
        this.load();
      },
      error: (err: unknown) => {
        this.saving.set(false);
        this.error.set(readError(err));
      },
    });
  }

  async remove(item: Supplier): Promise<void> {
    const ok = await this.confirm.ask('Delete supplier', `Delete “${item.name}”? Products that use it will block this.`);
    if (!ok) return;
    this.api.delete(item.id).subscribe({
      next: () => {
        this.notice.show('Supplier deleted.');
        this.load();
      },
      error: (err: unknown) => this.error.set(readError(err)),
    });
  }
}
