import { Injectable, signal } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class ConfirmService {
  readonly open = signal(false);
  readonly title = signal('Confirm');
  readonly message = signal('');
  private resolver: ((value: boolean) => void) | null = null;

  ask(title: string, message: string): Promise<boolean> {
    this.title.set(title);
    this.message.set(message);
    this.open.set(true);
    return new Promise((resolve) => {
      this.resolver = resolve;
    });
  }

  respond(value: boolean): void {
    this.open.set(false);
    this.resolver?.(value);
    this.resolver = null;
  }
}
