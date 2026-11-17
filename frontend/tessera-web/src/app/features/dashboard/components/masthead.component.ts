import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { UserDto } from '../../../core/models';
import { StreamStatus } from '../../../core/realtime/score-stream.service';

@Component({
  selector: 'ts-masthead',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <header class="mx-auto max-w-[1400px] px-4 sm:px-6 lg:px-10">
      <div class="flex flex-wrap items-center justify-between gap-2 border-b border-ink/25 py-2">
        <span class="kicker">Vol. I · No. {{ issueNumber }}</span>
        <span class="kicker hidden sm:inline">{{ dateline }}</span>
        <span class="kicker flex items-center gap-2" [attr.title]="'Realtime channel: ' + status()">
          <span class="inline-block h-2 w-2 rounded-full"
                [class.bg-verdigris]="status() === 'live'"
                [class.animate-blink]="status() === 'live' || status() === 'connecting' || status() === 'reconnecting'"
                [class.bg-ochre]="status() === 'connecting' || status() === 'reconnecting'"
                [class.bg-vermilion]="status() === 'offline'"></span>
          {{ statusLabel() }}
        </span>
      </div>

      <div class="flex flex-col gap-5 py-6 sm:flex-row sm:items-end sm:justify-between">
        <div>
          <h1 class="font-display text-[clamp(44px,7vw,88px)] font-semibold leading-[0.85] tracking-[-0.04em]" style="font-variation-settings:'opsz' 144">
            Tessera<span class="text-vermilion">.</span>
          </h1>
          <p class="mt-2 font-display text-lg italic text-ink-soft">The Personal Ledger &amp; Credit Gazette</p>
        </div>

        @if (user(); as u) {
          <div class="flex items-end gap-5 sm:text-right">
            <div>
              <p class="kicker">Account holder</p>
              <p class="font-display text-2xl leading-tight">{{ u.fullName }}</p>
              <p class="font-mono text-xs text-ink-soft">{{ u.email }}</p>
            </div>
            <button type="button" class="ghost-btn" (click)="signOut.emit()">Sign out</button>
          </div>
        }
      </div>
      <div class="rule-double"></div>
    </header>
  `
})
export class MastheadComponent {
  readonly user = input<UserDto | null>(null);
  readonly status = input<StreamStatus>('offline');
  readonly signOut = output<void>();

  protected readonly issueNumber = String(dayOfYear(new Date())).padStart(4, '0');
  protected readonly dateline = new Date()
    .toLocaleDateString('en-GB', { weekday: 'long', day: 'numeric', month: 'long', year: 'numeric' })
    .toUpperCase();

  protected readonly statusLabel = computed(() => ({
    live: 'Live wire',
    connecting: 'Dialling…',
    reconnecting: 'Re-dialling…',
    offline: 'Wire down'
  })[this.status()]);
}

function dayOfYear(date: Date): number {
  const start = Date.UTC(date.getFullYear(), 0, 0);
  return Math.floor((Date.UTC(date.getFullYear(), date.getMonth(), date.getDate()) - start) / 86_400_000);
}
