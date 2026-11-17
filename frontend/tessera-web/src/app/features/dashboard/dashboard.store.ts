import { DestroyRef, Injectable, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Observable, forkJoin, tap } from 'rxjs';
import { ScoreApi, WalletApi } from '../../core/api/wallet.api';
import {
  CreditReport,
  InstrumentKind,
  LedgerEntry,
  PagedResult,
  TransactionReceipt,
  WalletSummary
} from '../../core/models';
import { ScoreStreamService } from '../../core/realtime/score-stream.service';

export interface WireItem {
  id: string;
  label: string;
  value: string;
  tone: 'up' | 'down' | 'flat';
}

export interface InstrumentRequest {
  kind: InstrumentKind;
  amount: number;
  memo?: string;
  recipientEmail?: string;
}

/**
 * Component-scoped state for the ledger page (provided by DashboardPage).
 * Signals hold the view model; RxJS handles transport. Score pushes refresh the wallet too,
 * so incoming transfers from other users appear without polling.
 */
@Injectable()
export class DashboardStore {
  private readonly walletApi = inject(WalletApi);
  private readonly scoreApi = inject(ScoreApi);
  private readonly stream = inject(ScoreStreamService);
  private readonly destroyRef = inject(DestroyRef);

  readonly pageSize = 8;

  readonly summary = signal<WalletSummary | null>(null);
  readonly ledger = signal<PagedResult<LedgerEntry> | null>(null);
  readonly report = signal<CreditReport | null>(null);
  readonly page = signal(1);
  readonly loading = signal(true);
  readonly lastReceipt = signal<TransactionReceipt | null>(null);
  readonly pulse = signal(0);

  readonly streamStatus = this.stream.status;

  readonly wire = computed<WireItem[]>(() => {
    const items: WireItem[] = [];
    const report = this.report();
    if (report) {
      items.push({
        id: 'score',
        label: 'TESSERA SCORE',
        value: `${report.score} ${report.lastDelta > 0 ? '▲' : report.lastDelta < 0 ? '▼' : '■'} ${signed(report.lastDelta)}`,
        tone: report.lastDelta > 0 ? 'up' : report.lastDelta < 0 ? 'down' : 'flat'
      });
    }
    for (const entry of this.ledger()?.items ?? []) {
      items.push({
        id: entry.id,
        label: entry.reference,
        value: `${entry.direction === 'Credit' ? '+' : '−'}${entry.amount.toLocaleString('en-US', { minimumFractionDigits: 2 })}`,
        tone: entry.direction === 'Credit' ? 'up' : 'down'
      });
    }
    for (const snap of (report?.history ?? []).slice(-4).reverse()) {
      items.push({ id: snap.recordedAtUtc, label: snap.reason.toUpperCase(), value: signed(snap.delta), tone: snap.delta > 0 ? 'up' : snap.delta < 0 ? 'down' : 'flat' });
    }
    return items;
  });

  init(): void {
    this.loading.set(true);
    forkJoin({
      summary: this.walletApi.summary(),
      ledger: this.walletApi.ledger(1, this.pageSize),
      report: this.scoreApi.mine()
    }).subscribe({
      next: ({ summary, ledger, report }) => {
        this.summary.set(summary);
        this.ledger.set(ledger);
        this.report.set(report);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });

    void this.stream.connect();
    this.stream.updates$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(report => {
      this.report.set(report);
      this.pulse.update(n => n + 1);
      this.refreshWallet();
    });
  }

  goToPage(page: number): void {
    const total = this.ledger()?.totalPages ?? 1;
    if (page < 1 || page > Math.max(total, 1)) return;
    this.page.set(page);
    this.walletApi.ledger(page, this.pageSize).subscribe(ledger => this.ledger.set(ledger));
  }

  execute(request: InstrumentRequest): Observable<TransactionReceipt> {
    const call =
      request.kind === 'deposit'
        ? this.walletApi.deposit(request.amount, request.memo)
        : request.kind === 'withdraw'
          ? this.walletApi.withdraw(request.amount, request.memo)
          : this.walletApi.transfer(request.recipientEmail ?? '', request.amount, request.memo);

    return call.pipe(
      tap(receipt => {
        this.lastReceipt.set(receipt);
        this.page.set(1);
        this.refreshWallet();
      })
    );
  }

  /** Re-reads balance + first ledger page (cheap: the summary is served from Redis). */
  refreshWallet(): void {
    forkJoin({
      summary: this.walletApi.summary(),
      ledger: this.walletApi.ledger(this.page(), this.pageSize)
    }).subscribe(({ summary, ledger }) => {
      this.summary.set(summary);
      this.ledger.set(ledger);
    });
  }
}

function signed(n: number): string {
  return n > 0 ? `+${n}` : n < 0 ? `−${Math.abs(n)}` : '±0';
}
