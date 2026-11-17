import { Injectable, NgZone, OnDestroy, inject, signal } from '@angular/core';
import { HubConnection, HubConnectionBuilder, HubConnectionState, LogLevel } from '@microsoft/signalr';
import { Observable, Subject } from 'rxjs';
import { SessionStore } from '../auth/session.store';
import { API_BASE_URL, IS_DEMO } from '../config';
import { DemoBackend } from '../demo/demo-backend';
import { CreditReport } from '../models';

export type StreamStatus = 'offline' | 'connecting' | 'live' | 'reconnecting';

/**
 * Live credit-score feed. Scoring.Service pushes `scoreUpdated` over SignalR (through the gateway)
 * every time it consumes a wallet event from RabbitMQ.
 */
@Injectable({ providedIn: 'root' })
export class ScoreStreamService implements OnDestroy {
  private readonly session = inject(SessionStore);
  private readonly baseUrl = inject(API_BASE_URL);
  private readonly isDemo = inject(IS_DEMO);
  private readonly demo = inject(DemoBackend);
  private readonly zone = inject(NgZone);

  private connection?: HubConnection;
  private readonly updates = new Subject<CreditReport>();
  private demoSubscription?: { unsubscribe(): void };

  readonly status = signal<StreamStatus>('offline');
  readonly updates$: Observable<CreditReport> = this.updates.asObservable();

  async connect(): Promise<void> {
    if (this.isDemo) {
      this.demoSubscription ??= this.demo.scoreUpdates$.subscribe(report => this.updates.next(report));
      this.status.set('live');
      return;
    }

    if (this.connection && this.connection.state !== HubConnectionState.Disconnected) {
      return;
    }

    this.connection = new HubConnectionBuilder()
      .withUrl(`${this.baseUrl}/hubs/score`, {
        // Browsers cannot set headers on WebSocket upgrades; the token travels as ?access_token=.
        accessTokenFactory: () => this.session.accessToken() ?? ''
      })
      .withAutomaticReconnect([0, 2_000, 5_000, 10_000, 30_000])
      .configureLogging(LogLevel.Warning)
      .build();

    this.connection.on('scoreUpdated', (report: CreditReport) => this.zone.run(() => this.updates.next(report)));
    this.connection.onreconnecting(() => this.zone.run(() => this.status.set('reconnecting')));
    this.connection.onreconnected(() => this.zone.run(() => this.status.set('live')));
    this.connection.onclose(() => this.zone.run(() => this.status.set('offline')));

    this.status.set('connecting');
    try {
      await this.connection.start();
      this.status.set('live');
    } catch {
      this.status.set('offline');
    }
  }

  async disconnect(): Promise<void> {
    this.demoSubscription?.unsubscribe();
    this.demoSubscription = undefined;
    await this.connection?.stop();
    this.status.set('offline');
  }

  ngOnDestroy(): void {
    void this.disconnect();
  }
}
