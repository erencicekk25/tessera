import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { ScoreFactor } from '../../../core/models';
import { SignedPipe } from '../../../shared/formatting';

/**
 * Explains the score: each factor's contribution drawn on its own min→max scale with the zero
 * point marked, so positive and negative weights read at a glance.
 */
@Component({
  selector: 'ts-score-drivers',
  standalone: true,
  imports: [SignedPipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <h2 class="kicker"><span class="section-mark mr-2 text-base normal-case tracking-normal">§5</span>What moves the dial</h2>
    <p class="mt-2 font-display text-[15px] italic text-ink-soft">Baseline 600, plus or minus these five columns.</p>

    <ol class="mt-5 space-y-5">
      @for (f of factors(); track f.key; let i = $index) {
        <li>
          <div class="flex items-baseline justify-between gap-3">
            <p class="font-display text-[17px]"><span class="mr-2 font-mono text-[11px] text-ink-faint">{{ roman[i] }}</span>{{ f.label }}</p>
            <p class="font-mono text-[15px] font-semibold"
               [class.text-verdigris]="f.points > 0" [class.text-vermilion]="f.points < 0" [class.text-ink-faint]="f.points === 0">
              {{ f.points | signed }}
            </p>
          </div>

          <div class="relative mt-2 h-[10px] border border-ink/60 bg-paper" aria-hidden="true">
            <div class="absolute inset-y-0 transition-all duration-700"
                 [class.bg-verdigris]="f.points > 0" [class.hatch-vermilion]="f.points < 0"
                 [style.left.%]="segmentLeft(f)" [style.width.%]="segmentWidth(f)"></div>
            <div class="absolute -bottom-[4px] -top-[4px] w-[2px] bg-ink" [style.left.%]="zero(f)"></div>
          </div>
          <div class="mt-1 flex justify-between font-mono text-[9.5px] text-ink-faint">
            <span>{{ f.minPoints | signed }}</span><span>{{ f.maxPoints | signed }}</span>
          </div>

          <p class="mt-1 text-[13px] leading-snug text-ink-soft">{{ f.explanation }}</p>
        </li>
      }
    </ol>
  `
})
export class ScoreDriversComponent {
  readonly factors = input<ScoreFactor[]>([]);

  protected readonly roman = ['I', 'II', 'III', 'IV', 'V', 'VI'];

  protected zero(f: ScoreFactor): number {
    return this.pct(f, 0);
  }

  protected segmentLeft(f: ScoreFactor): number {
    return Math.min(this.pct(f, 0), this.pct(f, f.points));
  }

  protected segmentWidth(f: ScoreFactor): number {
    return Math.abs(this.pct(f, f.points) - this.pct(f, 0));
  }

  private pct(f: ScoreFactor, value: number): number {
    const span = f.maxPoints - f.minPoints || 1;
    return ((value - f.minPoints) / span) * 100;
  }
}
