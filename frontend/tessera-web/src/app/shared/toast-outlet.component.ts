import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { ToastService } from '../core/notify/toast.service';

/** Notifications printed as telegram slips in the lower-left corner. */
@Component({
  selector: 'ts-toast-outlet',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="pointer-events-none fixed bottom-4 left-4 right-4 z-50 flex flex-col gap-3 sm:right-auto sm:w-[380px]" aria-live="polite">
      @for (toast of toasts.toasts(); track toast.id) {
        <div class="pointer-events-auto animate-slip-in border-2 border-l-[10px] border-ink bg-paper shadow-slip"
             [style.border-left-color]="accent(toast.tone)">
          <div class="flex items-start justify-between gap-4 px-4 py-3">
            <div>
              <p class="kicker">{{ label(toast.tone) }}</p>
              <p class="mt-1 font-display text-lg leading-tight">{{ toast.title }}</p>
              <p class="mt-1 text-sm text-ink-soft">{{ toast.body }}</p>
            </div>
            <button type="button" class="font-mono text-xs text-ink-faint hover:text-ink" (click)="toasts.dismiss(toast.id)" aria-label="Dismiss">✕</button>
          </div>
        </div>
      }
    </div>
  `
})
export class ToastOutletComponent {
  protected readonly toasts = inject(ToastService);

  protected accent(tone: string): string {
    return { info: '#1A1714', success: '#1F6F5C', warn: '#B9850F', error: '#C8361D' }[tone] ?? '#1A1714';
  }

  protected label(tone: string): string {
    return { info: 'Wire', success: 'Posted', warn: 'Notice', error: 'Stop press' }[tone] ?? 'Wire';
  }
}
