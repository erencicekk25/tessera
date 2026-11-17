import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { WalletSummary } from '../../../core/models';
import { AmountPipe, splitAmount } from '../../../shared/formatting';

@Component({
  selector: 'ts-balance-panel',
  standalone: true,
  imports: [AmountPipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (summary(); as s) {
      <h2 class="kicker"><span class="section-mark mr-2 text-base normal-case tracking-normal">§1</span>Closing balance</h2>

      <p class="mt-5 flex items-start font-display font-light leading-[0.82] tracking-[-0.045em] figures" style="font-variation-settings:'opsz' 144">
        <span class="mr-2 mt-[0.35em] font-mono text-xl font-normal tracking-normal text-ink-soft">₺</span>
        <span class="text-[clamp(60px,8.5vw,118px)]">{{ parts().whole }}</span>
        <span class="mt-[0.18em] text-[clamp(26px,3.2vw,44px)] text-ink-soft">.{{ parts().cents }}</span>
      </p>
      <p class="mt-3 font-mono text-[11px] text-ink-faint">
        {{ s.currency }} · {{ s.entryCount }} entries posted · last movement {{ lastMovement() }}
      </p>

      <div class="mt-8 grid grid-cols-2 gap-6 rule-hair pt-4">
        <div>
          <p class="kicker">Total credits</p>
          <p class="mt-1 font-mono text-xl text-verdigris">+{{ s.totalInflow | amount }}</p>
        </div>
        <div>
          <p class="kicker">Total debits</p>
          <p class="mt-1 font-mono text-xl text-vermilion">−{{ s.totalOutflow | amount }}</p>
        </div>
      </div>

      <div class="mt-4" aria-hidden="true">
        <div class="flex h-3 w-full border border-ink">
          <div class="bg-verdigris transition-[width] duration-700" [style.width.%]="creditShare()"></div>
          <div class="hatch-vermilion flex-1"></div>
        </div>
        <div class="mt-1 flex justify-between font-mono text-[10px] text-ink-faint">
          <span>IN {{ creditShare() }}%</span>
          <span>OUT {{ 100 - creditShare() }}%</span>
        </div>
      </div>

      <dl class="mt-8 space-y-2 rule-hair pt-4 font-mono text-[11.5px]">
        <div class="flex justify-between gap-4"><dt class="text-ink-faint">Wallet no.</dt><dd class="truncate">{{ walletNumber() }}</dd></div>
        <div class="flex justify-between gap-4"><dt class="text-ink-faint">Opened</dt><dd>{{ opened() }}</dd></div>
        <div class="flex justify-between gap-4"><dt class="text-ink-faint">Cache</dt><dd>Redis · evicted on every posting</dd></div>
      </dl>
    } @else {
      <div class="h-[420px] animate-pulse bg-paper-deep/60"></div>
    }
  `
})
export class BalancePanelComponent {
  readonly summary = input<WalletSummary | null>(null);

  protected readonly parts = computed(() => splitAmount(this.summary()?.balance ?? 0));

  protected readonly creditShare = computed(() => {
    const s = this.summary();
    const total = (s?.totalInflow ?? 0) + (s?.totalOutflow ?? 0);
    return total === 0 ? 50 : Math.round(((s?.totalInflow ?? 0) / total) * 100);
  });

  protected readonly walletNumber = computed(() =>
    (this.summary()?.walletId ?? '').replace(/-/g, '').toUpperCase().match(/.{1,4}/g)?.slice(0, 4).join(' · ') ?? '');

  protected readonly opened = computed(() => {
    const s = this.summary();
    return s ? new Date(s.openedAtUtc).toLocaleDateString('en-GB', { day: 'numeric', month: 'long', year: 'numeric' }) : '';
  });

  protected readonly lastMovement = computed(() => {
    const at = this.summary()?.lastActivityAtUtc;
    return at ? new Date(at).toLocaleString('en-GB', { day: '2-digit', month: 'short', hour: '2-digit', minute: '2-digit' }) : 'never';
  });
}
