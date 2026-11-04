import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { CategoryService } from '../../core/category.service';
import { readError } from '../../core/format';
import { Category, Supplier } from '../../core/models';
import { NoticeService } from '../../core/notice.service';
import { ProductService } from '../../core/product.service';
import { SupplierService } from '../../core/supplier.service';

@Component({
  selector: 'app-product-form',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './product-form.component.html',
})
export class ProductFormComponent {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly products = inject(ProductService);
  private readonly categoriesApi = inject(CategoryService);
  private readonly suppliersApi = inject(SupplierService);
  private readonly formBuilder = inject(FormBuilder);
  private readonly notice = inject(NoticeService);

  readonly categories = signal<Category[]>([]);
  readonly suppliers = signal<Supplier[]>([]);
  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly error = signal('');
  readonly productId = signal<number | null>(null);
  readonly currentStock = signal<number | null>(null);
  readonly form = this.formBuilder.nonNullable.group({
    sku: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(50)]],
    name: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(150)]],
    description: ['', Validators.maxLength(500)],
    unitPrice: [0, [Validators.required, Validators.min(0)]],
    reorderLevel: [0, [Validators.required, Validators.min(0)]],
    categoryId: ['', Validators.required],
    supplierId: ['', Validators.required],
    isActive: true,
  });

  constructor() {
    const idParam = this.route.snapshot.paramMap.get('id');
    const id = idParam ? Number(idParam) : null;
    this.productId.set(id);
    if (id) this.form.controls.sku.disable();

    this.categoriesApi.list().subscribe({
      next: (items) => this.categories.set(items),
      error: (err: unknown) => this.error.set(readError(err)),
    });
    this.suppliersApi.list().subscribe({
      next: (items) => this.suppliers.set(items),
      error: (err: unknown) => this.error.set(readError(err)),
    });

    if (!id) {
      this.loading.set(false);
      return;
    }

    this.products.get(id).subscribe({
      next: (product) => {
        this.currentStock.set(product.quantityInStock);
        this.form.patchValue({
          sku: product.sku,
          name: product.name,
          description: product.description ?? '',
          unitPrice: product.unitPrice,
          reorderLevel: product.reorderLevel,
          categoryId: String(product.categoryId),
          supplierId: String(product.supplierId),
          isActive: product.isActive,
        });
        this.loading.set(false);
      },
      error: (err: unknown) => {
        this.error.set(readError(err));
        this.loading.set(false);
      },
    });
  }

  save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const value = this.form.getRawValue();
    const body = {
      sku: value.sku.trim(),
      name: value.name.trim(),
      description: value.description.trim() || null,
      unitPrice: Number(value.unitPrice),
      reorderLevel: Number(value.reorderLevel),
      categoryId: Number(value.categoryId),
      supplierId: Number(value.supplierId),
      isActive: value.isActive,
    };

    this.saving.set(true);
    this.error.set('');
    const id = this.productId();
    const request = id ? this.products.update(id, body) : this.products.create(body);
    request.subscribe({
      next: () => {
        this.saving.set(false);
        this.notice.show(id ? 'Product updated. Stock quantity was left unchanged.' : 'Product created with zero stock.');
        void this.router.navigate(['/products']);
      },
      error: (err: unknown) => {
        this.saving.set(false);
        this.error.set(readError(err));
      },
    });
  }
}
