import { Component, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { AuthService } from '../../core/auth.service';
import { isLowStock, localDate, money, readError } from '../../core/format';
import { Product } from '../../core/models';
import { ProductService } from '../../core/product.service';

@Component({
  selector: 'app-product-detail',
  imports: [RouterLink],
  templateUrl: './product-detail.component.html',
})
export class ProductDetailComponent {
  readonly auth = inject(AuthService);
  private readonly route = inject(ActivatedRoute);
  private readonly products = inject(ProductService);

  readonly loading = signal(true);
  readonly error = signal('');
  readonly product = signal<Product | null>(null);
  readonly money = money;
  readonly localDate = localDate;
  readonly isLowStock = isLowStock;

  constructor() {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    this.products.get(id).subscribe({
      next: (product) => {
        this.product.set(product);
        this.loading.set(false);
      },
      error: (err: unknown) => {
        this.error.set(readError(err));
        this.loading.set(false);
      },
    });
  }
}
