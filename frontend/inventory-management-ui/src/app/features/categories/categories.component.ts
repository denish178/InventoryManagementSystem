import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { AuthService } from '../../core/auth.service';
import { CategoryService } from '../../core/category.service';
import { readError } from '../../core/format';
import { Category } from '../../core/models';

@Component({
  selector: 'app-categories',
  imports: [ReactiveFormsModule],
  templateUrl: './categories.component.html',
})
export class CategoriesComponent {
  readonly auth = inject(AuthService);
  private readonly api = inject(CategoryService);
  private readonly formBuilder = inject(FormBuilder);

  readonly items = signal<Category[]>([]);
  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly error = signal('');
  readonly editingId = signal<number | null>(null);
  readonly form = this.formBuilder.nonNullable.group({
    name: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(100)]],
    description: ['', Validators.maxLength(500)],
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

  edit(item: Category): void {
    this.editingId.set(item.id);
    this.form.setValue({
      name: item.name,
      description: item.description ?? '',
      isActive: item.isActive,
    });
  }

  cancel(): void {
    this.editingId.set(null);
    this.form.reset({ name: '', description: '', isActive: true });
  }

  save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.saving.set(true);
    this.error.set('');
    const value = this.form.getRawValue();
    const description = value.description.trim() || null;
    const request = this.editingId()
      ? this.api.update(this.editingId()!, { name: value.name.trim(), description, isActive: value.isActive })
      : this.api.create({ name: value.name.trim(), description });

    request.subscribe({
      next: () => {
        this.saving.set(false);
        this.cancel();
        this.load();
      },
      error: (err: unknown) => {
        this.saving.set(false);
        this.error.set(readError(err));
      },
    });
  }

  remove(item: Category): void {
    if (!confirm(`Delete category “${item.name}”?`)) return;
    this.api.delete(item.id).subscribe({
      next: () => this.load(),
      error: (err: unknown) => this.error.set(readError(err)),
    });
  }
}
