import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { ScoreSnapshot } from '../../../core/models';

const W = 640;
const H = 150;
const PAD = { top: 14, right: 44, bottom: 22, left: 8 };

/** Step chart of the score timeline, with band thresholds ruled in and declines marked in red. */
@Component({
  selector: 'ts-score-trend',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <svg [attr.viewBox]="'0 0 ' + W + ' ' + H" class="w-full" role="img" aria-label="Score history">
      @for (line of thresholds(); track line.value) {
        <line [attr.x1]="PAD.left" [attr.x2]="W - PAD.right" [attr.y1]="line.y" [attr.y2]="line.y"
              stroke="#1A1714" stroke-opacity=".22" stroke-dasharray="2 4" />
        <text [attr.x]="W - PAD.right + 6" [attr.y]="line.y" dominant-baseline="middle" class="fill-ink-faint font-mono" style="font-size:9px">{{ line.value }}</text>
      }

      <line [attr.x1]="PAD.left" [attr.x2]="W - PAD.right" [attr.y1]="H - PAD.bottom" [attr.y2]="H - PAD.bottom" stroke="#1A1714" stroke-width="1.2" />

      <path [attr.d]="area()" fill="#1A1714" fill-opacity=".05" />
      <path [attr.d]="path()" fill="none" stroke="#1A1714" stroke-width="1.8" stroke-linejoin="miter" />

      @for (p of points(); track $index; let last = $last) {
        <circle [attr.cx]="p.x" [attr.cy]="p.y" [attr.r]="last ? 4.5 : p.delta < 0 ? 3.4 : 2.4"
                [attr.fill]="p.delta < 0 ? '#C8361D' : last ? '#1A1714' : '#F2EDE3'"
                [attr.stroke]="p.delta < 0 ? '#C8361D' : '#1A1714'" stroke-width="1.4">
          <title>{{ p.score }} · {{ p.reason }}</title>
        </circle>
      }

      @if (points().length) {
        <text [attr.x]="PAD.left" [attr.y]="H - 6" class="fill-ink-faint font-mono" style="font-size:9px">{{ firstLabel() }}</text>
        <text [attr.x]="W - PAD.right" [attr.y]="H - 6" text-anchor="end" class="fill-ink-faint font-mono" style="font-size:9px">{{ lastLabel() }}</text>
      }
    </svg>
  `
})
export class ScoreTrendComponent {
  readonly history = input<ScoreSnapshot[]>([]);

  protected readonly W = W;
  protected readonly H = H;
  protected readonly PAD = PAD;

  private readonly domain = computed(() => {
    const scores = this.history().map(h => h.score);
    const lo = Math.max(300, Math.min(...scores, 600) - 25);
    const hi = Math.min(850, Math.max(...scores, 600) + 25);
    return { lo, hi };
  });

  protected readonly points = computed(() => {
    const history = this.history();
    const { lo, hi } = this.domain();
    const span = Math.max(history.length - 1, 1);
    return history.map((h, i) => ({
      x: PAD.left + (i / span) * (W - PAD.left - PAD.right),
      y: this.y(h.score, lo, hi),
      score: h.score,
      delta: h.delta,
      reason: h.reason
    }));
  });

  protected readonly path = computed(() => {
    const pts = this.points();
    if (!pts.length) return '';
    return pts.reduce((d, p, i) => (i === 0 ? `M ${p.x} ${p.y}` : `${d} H ${p.x} V ${p.y}`), '');
  });

  protected readonly area = computed(() => {
    const pts = this.points();
    if (!pts.length) return '';
    const base = H - PAD.bottom;
    return `${this.path()} V ${base} H ${pts[0].x} Z`;
  });

  protected readonly thresholds = computed(() => {
    const { lo, hi } = this.domain();
    return [580, 670, 740, 800].filter(v => v > lo && v < hi).map(value => ({ value, y: this.y(value, lo, hi) }));
  });

  protected readonly firstLabel = computed(() => stamp(this.history()[0]?.recordedAtUtc));
  protected readonly lastLabel = computed(() => stamp(this.history().at(-1)?.recordedAtUtc));

  private y(score: number, lo: number, hi: number): number {
    return PAD.top + (1 - (score - lo) / (hi - lo)) * (H - PAD.top - PAD.bottom);
  }
}

function stamp(iso?: string): string {
  return iso ? new Date(iso).toLocaleDateString('en-GB', { day: '2-digit', month: 'short' }).toUpperCase() : '';
}
