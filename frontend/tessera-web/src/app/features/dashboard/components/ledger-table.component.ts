import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { LedgerEntry, PagedResult } from '../../../core/models';
import { AmountPipe } from '../../../shared/formatting';

/** The general ledger, set like an accounting book: Dr / Cr columns, running balance, folios. */
@Component({
  selector: 'ts-ledger-table',
  standalone: true,
  imports: [AmountPipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="flex flex-wrap items-baseline justify-between gap-3">
      <h2 class="kicker"><span class="section-mark mr-2 text-base normal-case tracking-normal">§4</span>General ledger</h2>
      @if (ledger(); as l) {
        <div class="flex items-center gap-2">
          <button type="button" class="ghost-btn" [disabled]="l.page <= 1" (click)="pageChange.emit(l.page - 1)" aria-label="Previous folio">←</button>
          <span class="font-mono text-[11px] uppercase tracking-[0.18em]">Folio {{ l.page }} / {{ l.totalPages || 1 }}</span>
          <button type="button" class="ghost-btn" [disabled]="l.page >= l.totalPages" (click)="pageChange.emit(l.page + 1)" aria-label="Next folio">→</button>
        </div>
      }
    </div>

    <div class="mt-5 overflow-x-auto">
      <table class="w-full border-collapse text-left">
        <thead>
          <tr class="border-b-[3px] border-double border-ink font-mono text-[10px] uppercase tracking-[0.2em] text-ink-soft">
            <th class="hidden py-2 pr-4 font-normal sm:table-cell">Date</th>
            <th class="py-2 pr-4 font-normal">Particulars</th>
            <th class="hidden border-l border-ink/30 py-2 pl-3 pr-3 text-right font-normal md:table-cell">Debit</th>
            <th class="hidden py-2 pl-3 pr-3 text-right font-normal md:table-cell">Credit</th>
            <th class="border-l-[3px] border-double border-ink py-2 pl-3 text-right font-normal">Balance</th>
          </tr>
        </thead>
        <tbody>
          @for (e of ledger()?.items ?? []; track e.id) {
            <tr class="border-b border-ink/15 align-top transition-colors hover:bg-paper-deep/60"
                [class.bg-ochre-soft]="e.id === highlightId()">
              <td class="hidden whitespace-nowrap py-3 pr-4 sm:table-cell">
                <span class="block font-mono text-[12px] font-semibold">{{ day(e.postedAtUtc) }}</span>
                <span class="block font-mono text-[11px] text-ink-faint">{{ time(e.postedAtUtc) }}</span>
              </td>
              <td class="py-3 pr-4">
                <span class="block text-[15px] leading-snug">{{ e.description }}</span>
                <span class="mt-1 flex flex-wrap items-center gap-2 font-mono text-[10.5px] text-ink-faint">
                  <span class="border border-ink/40 px-1.5 py-[1px] uppercase tracking-[0.12em] text-ink-soft">{{ typeLabel(e) }}</span>
                  <span>{{ e.reference }}</span>
                  @if (e.counterparty) { <span>· {{ e.type === 'TransferOut' ? 'to' : 'from' }} {{ e.counterparty }}</span> }
                  <span class="sm:hidden">· {{ day(e.postedAtUtc) }}</span>
                </span>
                <span class="mt-1 block font-mono text-[13px] md:hidden"
                      [class.text-vermilion]="e.direction === 'Debit'" [class.text-verdigris]="e.direction === 'Credit'">
                  {{ e.direction === 'Debit' ? '−' : '+' }}{{ e.amount | amount }}
                </span>
              </td>
              <td class="hidden border-l border-ink/30 py-3 pl-3 pr-3 text-right font-mono text-[14px] text-vermilion figures md:table-cell">
                {{ e.direction === 'Debit' ? (e.amount | amount) : '' }}
              </td>
              <td class="hidden py-3 pl-3 pr-3 text-right font-mono text-[14px] text-verdigris figures md:table-cell">
                {{ e.direction === 'Credit' ? (e.amount | amount) : '' }}
              </td>
              <td class="border-l-[3px] border-double border-ink py-3 pl-3 text-right font-mono text-[14px] figures">
                {{ e.balanceAfter | amount }}
              </td>
            </tr>
          } @empty {
            <tr>
              <td colspan="5" class="py-14 text-center">
                <p class="font-display text-2xl italic">The book is blank.</p>
                <p class="mt-1 font-mono text-[12px] text-ink-faint">Post a deposit to open folio one.</p>
              </td>
            </tr>
          }
        </tbody>
        @if (ledger()?.items?.length) {
          <tfoot>
            <tr class="border-t-2 border-ink font-mono text-[11px] uppercase tracking-[0.18em]">
              <td class="hidden py-3 sm:table-cell"></td>
              <td class="py-3 text-ink-soft">{{ ledger()!.totalCount }} entries · carried forward</td>
              <td class="hidden md:table-cell"></td>
              <td class="hidden md:table-cell"></td>
              <td class="border-l-[3px] border-double border-ink py-3 pl-3 text-right text-[14px] font-semibold normal-case tracking-normal">
                {{ ledger()!.items[0].balanceAfter | amount }}
              </td>
            </tr>
          </tfoot>
        }
      </table>
    </div>
  `
})
export class LedgerTableComponent {
  readonly ledger = input<PagedResult<LedgerEntry> | null>(null);
  readonly highlightId = input<string | null>(null);
  readonly pageChange = output<number>();

  protected day(iso: string): string {
    return new Date(iso).toLocaleDateString('en-GB', { day: '2-digit', month: 'short' }).toUpperCase();
  }

  protected time(iso: string): string {
    return new Date(iso).toLocaleTimeString('en-GB', { hour: '2-digit', minute: '2-digit' });
  }

  protected typeLabel(e: LedgerEntry): string {
    return { Deposit: 'Deposit', Withdrawal: 'Withdrawal', TransferIn: 'Transfer in', TransferOut: 'Transfer out' }[e.type];
  }
}
