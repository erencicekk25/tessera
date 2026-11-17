import { ChangeDetectionStrategy, Component, DestroyRef, computed, effect, inject, input, signal, untracked } from '@angular/core';
import { ScoreBand } from '../../../core/models';

const CX = 170;
const CY = 170;
const R = 138;
const MIN = 300;
const MAX = 850;

interface BandArc {
  band: ScoreBand;
  from: number;
  to: number;
  stroke: string;
}

/**
 * Analog instrument dial: 300–850 sweep, banded arc, minor/major graduations and a needle that
 * swings (CSS transition) whenever a new score is pushed from the hub. The numeral counts up.
 */
@Component({
  selector: 'ts-score-gauge',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <figure class="relative mx-auto w-full max-w-[460px]">
      <svg viewBox="0 0 340 196" class="w-full overflow-visible" role="img" [attr.aria-label]="'Credit score ' + score() + ' of 850'">
        <defs>
          <pattern id="hatch-poor" width="5" height="5" patternUnits="userSpaceOnUse" patternTransform="rotate(45)">
            <rect width="5" height="5" fill="#F2D3C9" />
            <line x1="0" y1="0" x2="0" y2="5" stroke="#C8361D" stroke-width="2.2" />
          </pattern>
        </defs>

        <!-- Graduations -->
        @for (tick of ticks; track tick.value) {
          <line [attr.x1]="tick.x1" [attr.y1]="tick.y1" [attr.x2]="tick.x2" [attr.y2]="tick.y2"
                stroke="#1A1714" [attr.stroke-width]="tick.major ? 1.6 : 0.7" [attr.opacity]="tick.major ? 1 : 0.55" />
        }

        <!-- Band arcs -->
        @for (arc of arcs; track arc.band) {
          <path [attr.d]="arcPath(arc.from, arc.to, R)" fill="none" [attr.stroke]="arc.stroke" stroke-width="18"
                [attr.opacity]="arc.band === band() ? 1 : 0.32" style="transition: opacity 600ms ease" />
        }
        <path [attr.d]="arcPath(MIN, MAX, R + 9.5)" fill="none" stroke="#1A1714" stroke-width="1" />
        <path [attr.d]="arcPath(MIN, MAX, R - 9.5)" fill="none" stroke="#1A1714" stroke-width="1" />

        <!-- Band edge labels -->
        @for (label of labels; track label.value) {
          <text [attr.x]="label.x" [attr.y]="label.y" text-anchor="middle" dominant-baseline="middle"
                class="fill-ink font-mono" style="font-size:9.5px">{{ label.value }}</text>
        }

        <!-- Needle -->
        <g [style.transform]="'rotate(' + needleDeg() + 'deg)'"
           style="transform-origin: 170px 170px; transform-box: view-box; transition: transform 1.4s cubic-bezier(.34,1.45,.5,1)">
          <polygon [attr.points]="needlePoints" fill="#1A1714" />
          <circle [attr.cx]="CX - R + 50" [attr.cy]="CY" r="3.2" fill="#C8361D" />
        </g>
        <circle [attr.cx]="CX" [attr.cy]="CY" r="11" fill="#F2EDE3" stroke="#1A1714" stroke-width="2" />
        <circle [attr.cx]="CX" [attr.cy]="CY" r="3.5" fill="#1A1714" />
        <text x="22" y="190" class="fill-ink-faint font-mono" style="font-size:8px;letter-spacing:.18em">POOR</text>
        <text x="318" y="190" text-anchor="end" class="fill-ink-faint font-mono" style="font-size:8px;letter-spacing:.18em">EXCEPTIONAL</text>
      </svg>

      <figcaption class="mt-3 text-center">
        <span class="font-display text-[clamp(64px,9vw,104px)] font-semibold leading-none tracking-[-0.04em] figures" style="font-variation-settings:'opsz' 144">{{ displayed() }}</span>
        <span class="ml-1 align-top font-mono text-xs text-ink-faint">/ 850</span>
      </figcaption>
    </figure>
  `
})
export class ScoreGaugeComponent {
  readonly score = input.required<number>();
  readonly band = input.required<ScoreBand>();

  protected readonly CX = CX;
  protected readonly CY = CY;
  protected readonly R = R;
  protected readonly MIN = MIN;
  protected readonly MAX = MAX;

  protected readonly displayed = signal(MIN);
  protected readonly needleDeg = computed(() => ((clamp(this.score()) - MIN) / (MAX - MIN)) * 180);

  // The needle stops short of the band labels (drawn at R - 26) so it never hides them.
  protected readonly needlePoints = `${CX - R + 44},${CY} ${CX + 4},${CY - 4.5} ${CX + 16},${CY} ${CX + 4},${CY + 4.5}`;

  protected readonly arcs: BandArc[] = [
    { band: 'Poor', from: 300, to: 579, stroke: 'url(#hatch-poor)' },
    { band: 'Fair', from: 580, to: 669, stroke: '#B9850F' },
    { band: 'Good', from: 670, to: 739, stroke: '#4A443C' },
    { band: 'VeryGood', from: 740, to: 799, stroke: '#5E9C88' },
    { band: 'Exceptional', from: 800, to: 850, stroke: '#1F6F5C' }
  ];

  protected readonly ticks = Array.from({ length: (MAX - MIN) / 10 + 1 }, (_, i) => {
    const value = MIN + i * 10;
    const major = value % 50 === 0;
    const a = point(value, R + 12);
    const b = point(value, R + (major ? 22 : 17));
    return { value, major, x1: a.x, y1: a.y, x2: b.x, y2: b.y };
  });

  protected readonly labels = [300, 580, 670, 740, 800, 850].map(value => ({ value, ...point(value, R - 26) }));

  constructor() {
    let frame = 0;
    inject(DestroyRef).onDestroy(() => cancelAnimationFrame(frame));

    // Count the numeral towards each new score (ease-out, ~1.1 s).
    effect(() => {
      const target = clamp(this.score());
      const start = untracked(this.displayed);
      const began = performance.now();
      cancelAnimationFrame(frame);

      const step = (now: number) => {
        const t = Math.min((now - began) / 1100, 1);
        const eased = 1 - Math.pow(1 - t, 3);
        this.displayed.set(Math.round(start + (target - start) * eased));
        if (t < 1) frame = requestAnimationFrame(step);
      };
      frame = requestAnimationFrame(step);
    });
  }

  protected arcPath(from: number, to: number, radius: number): string {
    const a = point(from, radius);
    const b = point(to, radius);
    return `M ${a.x} ${a.y} A ${radius} ${radius} 0 0 1 ${b.x} ${b.y}`;
  }
}

function clamp(score: number): number {
  return Math.min(Math.max(score, MIN), MAX);
}

function point(score: number, radius: number): { x: number; y: number } {
  const angle = Math.PI * (1 - (score - MIN) / (MAX - MIN));
  return { x: +(CX + radius * Math.cos(angle)).toFixed(2), y: +(CY - radius * Math.sin(angle)).toFixed(2) };
}
