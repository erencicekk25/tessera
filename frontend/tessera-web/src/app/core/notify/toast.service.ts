import { Injectable, signal } from '@angular/core';

export type ToastTone = 'info' | 'success' | 'warn' | 'error';

export interface Toast {
  id: number;
  tone: ToastTone;
  title: string;
  body: string;
}

@Injectable({ providedIn: 'root' })
export class ToastService {
  private nextId = 1;
  readonly toasts = signal<Toast[]>([]);

  push(tone: ToastTone, title: string, body: string, ttlMs = 5200): void {
    const toast: Toast = { id: this.nextId++, tone, title, body };
    this.toasts.update(list => [...list.slice(-3), toast]);
    setTimeout(() => this.dismiss(toast.id), ttlMs);
  }

  dismiss(id: number): void {
    this.toasts.update(list => list.filter(t => t.id !== id));
  }
}
