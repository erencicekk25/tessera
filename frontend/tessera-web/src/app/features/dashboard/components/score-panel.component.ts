import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { CreditReport } from '../../../core/models';
import { AmountPipe, BandPipe, SignedPipe } from '../../../shared/formatting';
import { ScoreGaugeComponent } from './score-gauge.component';
import { ScoreTrendComponent } from './score-trend.component';

@Component({
  selector: 'ts-score-panel',
  standalone: true,
  imports: [ScoreGaugeComponent, ScoreTrendComponent, BandPipe, SignedPipe, AmountPipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (report(); as r) {
      <div class="flex items-baseline justify-between">
        <h2 class="kicker"><span class="section-mark mr-2 text-base normal-case tracking-normal">§2</span>Credit standing</h2>
        <span class="font-mono text-[11px] text-ink-faint">Re-rated {{ updated() }}</span>
      </div>

      <div class="mt-4 grid items-center gap-8 md:grid-cols-[minmax(0,1.15fr)_minmax(0,0.85fr)]">
        <ts-score-gauge [score]="r.score" [band]="r.band" />

        <div>
          <p class="kicker">Band</p>
          <p class="font-display text-5xl italic leading-none sm:text-6xl">{{ r.band | band }}</p>

          <div class="mt-6 flex items-center gap-4">
            @for (p of [pulse()]; track p) {
              <span class="inline-flex animate-stamp-in items-center border-2 px-3 py-1 font-mono text-xl font-semibold"
                    [class.border-verdigris]="r.lastDelta > 0" [class.text-verdigris]="r.lastDelta > 0"
                    [class.border-vermilion]="r.lastDelta < 0" [class.text-vermilion]="r.lastDelta < 0"
                    [class.border-ink]="r.lastDelta === 0">
                {{ r.lastDelta > 0 ? '▲' : r.lastDelta < 0 ? '▼' : '■' }}&nbsp;{{ r.lastDelta | signed }}
              </span>
            }
            <p class="text-sm leading-snug text-ink-soft">
              Last movement<br><span class="font-display text-base italic text-ink">{{ r.lastReason ?? 'No activity yet' }}</span>
            </p>
          </div>

          <dl class="mt-6 grid grid-cols-2 gap-x-6 gap-y-3 rule-hair pt-4">
            <div>
              <dt class="kicker">Settled txns</dt>
              <dd class="font-mono text-lg">{{ r.stats.transactionCount }}</dd>
            </div>
            <div>
              <dt class="kicker">Declined</dt>
              <dd class="font-mono text-lg" [class.text-vermilion]="r.stats.declinedCount > 0">{{ r.stats.declinedCount }}</dd>
            </div>
            <div>
              <dt class="kicker">Balance drains</dt>
              <dd class="font-mono text-lg" [class.text-vermilion]="r.stats.drainEventCount > 0">{{ r.stats.drainEventCount }}</dd>
            </div>
            <div>
              <dt class="kicker">Peak balance</dt>
              <dd class="font-mono text-lg">{{ r.stats.peakBalance | amount }}</dd>
            </div>
          </dl>
        </div>
      </div>

      <div class="mt-6 rule-hair pt-3">
        <div class="flex items-baseline justify-between">
          <p class="kicker">Score ticker · last {{ r.history.length }} re-ratings</p>
          <p class="font-mono text-[10.5px] text-ink-faint"><span class="text-vermilion">●</span> declined</p>
        </div>
        <ts-score-trend class="mt-2 block" [history]="r.history" />
      </div>
    } @else {
      <div class="h-[420px] animate-pulse bg-paper-deep/60"></div>
    }
  `
})
export class ScorePanelComponent {
  readonly report = input<CreditReport | null>(null);
  readonly pulse = input(0);

  protected readonly updated = computed(() => {
    const r = this.report();
    return r ? new Date(r.updatedAtUtc).toLocaleTimeString('en-GB', { hour: '2-digit', minute: '2-digit', second: '2-digit' }) : '';
  });
}
