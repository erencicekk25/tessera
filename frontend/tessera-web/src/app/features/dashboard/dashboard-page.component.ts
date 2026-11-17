import { ChangeDetectionStrategy, Component, OnInit, computed, inject } from '@angular/core';
import { AuthService } from '../../core/auth/auth.service';
import { SessionStore } from '../../core/auth/session.store';
import { IS_DEMO } from '../../core/config';
import { ScoreStreamService } from '../../core/realtime/score-stream.service';
import { BalancePanelComponent } from './components/balance-panel.component';
import { InstrumentsPanelComponent } from './components/instruments-panel.component';
import { LedgerTableComponent } from './components/ledger-table.component';
import { MastheadComponent } from './components/masthead.component';
import { ScoreDriversComponent } from './components/score-drivers.component';
import { ScorePanelComponent } from './components/score-panel.component';
import { WireTickerComponent } from './components/wire-ticker.component';
import { DashboardStore, InstrumentRequest } from './dashboard.store';

@Component({
  selector: 'ts-dashboard-page',
  standalone: true,
  imports: [
    MastheadComponent,
    WireTickerComponent,
    BalancePanelComponent,
    ScorePanelComponent,
    InstrumentsPanelComponent,
    ScoreDriversComponent,
    LedgerTableComponent
  ],
  providers: [DashboardStore],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <ts-masthead [user]="session.user()" [status]="store.streamStatus()" (signOut)="signOut()" />
    <ts-wire-ticker [items]="store.wire()" />

    <main class="mx-auto max-w-[1400px] px-4 pb-16 sm:px-6 lg:px-10">
      <!-- Front page: balance + score -->
      <section class="grid border-b-2 border-ink lg:grid-cols-12">
        <ts-balance-panel class="block py-8 lg:col-span-5 lg:border-r lg:border-ink/30 lg:pr-10" [summary]="store.summary()" />
        <ts-score-panel class="block border-t border-ink/30 py-8 lg:col-span-7 lg:border-t-0 lg:pl-10"
                        [report]="store.report()" [pulse]="store.pulse()" />
      </section>

      <!-- Inside pages: instruments, drivers, ledger -->
      <section class="grid lg:grid-cols-12">
        <aside class="py-8 lg:col-span-4 lg:border-r lg:border-ink/30 lg:pr-10">
          <ts-instruments-panel [execute]="execute" [knownRecipients]="knownRecipients" />
          <div class="mt-10 rule-thick pt-6">
            <ts-score-drivers [factors]="store.report()?.factors ?? []" />
          </div>
        </aside>

        <ts-ledger-table class="block border-t border-ink/30 py-8 lg:col-span-8 lg:border-t-0 lg:pl-10"
                         [ledger]="store.ledger()" [highlightId]="highlightId()" (pageChange)="store.goToPage($event)" />
      </section>

      <footer class="rule-double mt-2 flex flex-col gap-2 pt-4 font-mono text-[10.5px] uppercase tracking-[0.18em] text-ink-faint sm:flex-row sm:justify-between">
        <span>Printed by Wallet.Service · Scoring.Service · Gateway.Api</span>
        <span>{{ isDemo ? 'Demo edition — no money was harmed' : 'Simulation — not legal tender' }}</span>
      </footer>
    </main>
  `
})
export class DashboardPageComponent implements OnInit {
  protected readonly store = inject(DashboardStore);
  protected readonly session = inject(SessionStore);
  protected readonly isDemo = inject(IS_DEMO);
  private readonly auth = inject(AuthService);
  private readonly stream = inject(ScoreStreamService);

  protected readonly knownRecipients = ['grace@tessera.dev', 'alan@tessera.dev', 'ada@tessera.dev']
    .filter(email => email !== this.session.user()?.email);

  protected readonly highlightId = computed(() => this.store.lastReceipt()?.entryId ?? null);

  /** Passed to the instruments form as a callback so the form owns its own busy/error state. */
  protected readonly execute = (request: InstrumentRequest) => this.store.execute(request);

  ngOnInit(): void {
    this.store.init();
  }

  protected signOut(): void {
    void this.stream.disconnect();
    this.auth.signOut();
  }
}
