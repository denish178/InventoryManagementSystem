import { Injectable, signal } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class NoticeService {
  readonly message = signal('');
  readonly kind = signal<'ok' | 'error'>('ok');
  private timer = 0;

  show(message: string, kind: 'ok' | 'error' = 'ok'): void {
    this.kind.set(kind);
    this.message.set(message);
    window.clearTimeout(this.timer);
    this.timer = window.setTimeout(() => {
      if (this.message() === message) this.message.set('');
    }, 4500);
  }

  clear(): void {
    this.message.set('');
  }
}
