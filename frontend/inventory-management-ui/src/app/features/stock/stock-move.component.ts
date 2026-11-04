import { Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { ConfirmService } from '../../core/confirm.service';
import { readError } from '../../core/format';
import { Product } from '../../core/models';
import { NoticeService } from '../../core/notice.service';
import { ProductService } from '../../core/product.service';
import { StockService } from '../../core/stock.service';

@Component({
  selector: 'app-stock-move',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './stock-move.component.html',
})
export class StockMoveComponent {
  private readonly route = inject(ActivatedRoute);
  private readonly productsApi = inject(ProductService);
  private readonly stock = inject(StockService);
  private readonly formBuilder = inject(FormBuilder);
  private readonly confirm = inject(ConfirmService);
  private readonly notice = inject(NoticeService);
  private readonly destroyRef = inject(DestroyRef);

  readonly mode = this.route.snapshot.data['mode'] === 'out' ? 'out' : 'in';
  readonly products = signal<Product[]>([]);
  readonly selected = signal<Product | null>(null);
  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly error = signal('');
  readonly form = this.formBuilder.nonNullable.group({
    productId: ['', Validators.required],
    quantity: [1, [Validators.required, Validators.min(1), Validators.pattern(/^\d+$/)]],
    referenceNumber: ['', Validators.maxLength(100)],
    notes: ['', Validators.maxLength(500)],
  });

  constructor() {
    this.productsApi.listAll().subscribe({
      next: (items) => {
        this.products.set(items.filter((item) => item.isActive));
        this.loading.set(false);
      },
      error: (err: unknown) => {
        this.error.set(readError(err));
        this.loading.set(false);
      },
    });

    this.form.controls.productId.valueChanges.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((id) => {
      const product = this.products().find((item) => item.id === Number(id)) ?? null;
      this.selected.set(product);
    });
  }

  exceedsStock(): boolean {
    const product = this.selected();
    if (this.mode !== 'out' || !product) return false;
    return Number(this.form.controls.quantity.value) > product.quantityInStock;
  }

  async submit(): Promise<void> {
    if (this.form.invalid || this.exceedsStock()) {
      this.form.markAllAsTouched();
      return;
    }

    const value = this.form.getRawValue();
    const product = this.selected();
    const quantity = Number(value.quantity);
    if (this.mode === 'out') {
      const ok = await this.confirm.ask(
        'Confirm stock out',
        `Issue ${quantity} unit(s) of ${product?.name ?? 'this product'}? Available stock is ${product?.quantityInStock ?? 0}.`,
      );
      if (!ok) return;
    }

    const body = {
      productId: Number(value.productId),
      quantity,
      referenceNumber: value.referenceNumber.trim() || null,
      notes: value.notes.trim() || null,
    };

    this.saving.set(true);
    this.error.set('');
    const request = this.mode === 'in' ? this.stock.stockIn(body) : this.stock.stockOut(body);
    request.subscribe({
      next: (movement) => {
        this.saving.set(false);
        this.notice.show(
          this.mode === 'in'
            ? `Stock in recorded for ${movement.productName}.`
            : `Stock out recorded for ${movement.productName}.`,
        );
        this.refreshSelected(body.productId, this.mode === 'in' ? quantity : -quantity);
        this.form.patchValue({ quantity: 1, referenceNumber: '', notes: '' });
      },
      error: (err: unknown) => {
        this.saving.set(false);
        this.error.set(readError(err));
        this.refreshSelected(body.productId, 0);
      },
    });
  }

  private refreshSelected(productId: number, delta: number): void {
    this.productsApi.get(productId).subscribe({
      next: (product) => {
        this.products.update((items) => items.map((item) => (item.id === product.id ? product : item)));
        this.selected.set(product);
      },
      error: () => {
        if (delta !== 0) {
          this.products.update((items) =>
            items.map((item) =>
              item.id === productId ? { ...item, quantityInStock: item.quantityInStock + delta } : item,
            ),
          );
          const current = this.products().find((item) => item.id === productId) ?? null;
          this.selected.set(current);
        }
      },
    });
  }
}
