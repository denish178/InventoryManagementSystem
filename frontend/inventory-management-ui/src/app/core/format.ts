import { HttpErrorResponse } from '@angular/common/http';

export function readError(error: unknown): string {
  if (!(error instanceof HttpErrorResponse)) {
    return 'Something went wrong.';
  }

  if (error.status === 0) {
    return 'The API is unreachable. Start the backend and try again.';
  }

  const body = error.error as
    | { message?: string; title?: string; errors?: Record<string, string[]> | string[] }
    | string
    | null;

  if (typeof body === 'string' && body.trim()) {
    return body;
  }

  if (body && typeof body === 'object') {
    if (body.message) return body.message;
    if (Array.isArray(body.errors) && body.errors.length) {
      return String(body.errors[0]);
    }
    if (body.errors && !Array.isArray(body.errors)) {
      const first = Object.values(body.errors).find((messages) => messages?.length);
      if (first?.[0]) return first[0];
    }
    if (body.title) return body.title;
  }

  return `Request failed (${error.status}).`;
}

export function localDate(value: string | null | undefined): string {
  if (!value) return '—';
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return value;
  return date.toLocaleString();
}

export function money(value: number): string {
  return value.toLocaleString(undefined, {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  });
}

export function isLowStock(quantity: number, reorderLevel: number): boolean {
  return quantity <= reorderLevel;
}
