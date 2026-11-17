import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { WireItem } from '../dashboard.store';

/** Stock-ticker style band of the latest postings and score moves. */
@Component({
  selector: 'ts-wire-ticker',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="mx-auto max-w-[1400px] px-4 sm:px-6 lg:px-10">
      <div class="mt-[3px] flex items-stretch overflow-hidden bg-ink text-paper">
        <span class="z-10 flex shrink-0 items-center bg-vermilion px-3 font-mono text-[11px] font-semibold uppercase tracking-[0.25em]">Wire</span>
        <div class="relative flex-1 overflow-hidden py-2">
          <div class="flex w-max animate-ticker gap-10 whitespace-nowrap pl-6 font-mono text-[12px] hover:[animation-play-state:paused]">
            @for (item of loop(); track $index) {
              <span class="flex items-center gap-2">
                <span class="text-paper/60">{{ item.label }}</span>
                <span [style.color]="item.tone === 'up' ? '#7FD1B9' : item.tone === 'down' ? '#FF9B85' : null">{{ item.value }}</span>
                <span class="text-paper/30">◆</span>
              </span>
            }
          </div>
        </div>
      </div>
    </div>
  `
})
export class WireTickerComponent {
  readonly items = input<WireItem[]>([]);

  /** Duplicated so the -50% translate loops seamlessly. */
  protected readonly loop = computed(() => {
    const items = this.items().length ? this.items() : [{ id: 'idle', label: 'WIRE', value: 'awaiting first posting', tone: 'flat' as const }];
    return [...items, ...items];
  });
}
