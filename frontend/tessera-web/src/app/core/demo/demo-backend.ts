import { Injectable } from '@angular/core';
import { Subject } from 'rxjs';
import { CreditReport, LedgerEntry, LedgerEntryType, ScoreSnapshot, TransactionReceipt, WalletSummary } from '../models';
import { DRAIN_THRESHOLD, MAX_SCORE, MIN_SCORE, evaluate } from './scoring-policy';

interface DemoUser {
  id: string;
  email: string;
  fullName: string;
  password: string;
}

interface DemoWallet {
  walletId: string;
  balance: number;
  openedAtUtc: string;
  entries: LedgerEntry[];
}

interface DemoProfile {
  displayName: string;
  score: number;
  totalInflow: number;
  totalOutflow: number;
  lastKnownBalance: number;
  peakBalance: number;
  transactionCount: number;
  declinedCount: number;
  drainEventCount: number;
  history: ScoreSnapshot[];
  updatedAtUtc: string;
}

interface DemoState {
  users: DemoUser[];
  wallets: Record<string, DemoWallet>;
  profiles: Record<string, DemoProfile>;
}

export class DemoFailure {
  constructor(readonly status: number, readonly title: string, readonly detail: string, readonly code: string) {}
}

const STORAGE_KEY = 'tessera.demo.v1';
export const DEMO_ACCOUNT = { email: 'ada@tessera.dev', password: 'Ledger2026' };

/**
 * In-browser stand-in for Wallet.Service + Scoring.Service. Mirrors the server rules (ledger
 * invariants, declined-payment events, asynchronous re-scoring) closely enough to make the UI honest.
 */
@Injectable({ providedIn: 'root' })
export class DemoBackend {
  private state: DemoState = this.load();
  private readonly scoreUpdates = new Subject<CreditReport>();
  readonly scoreUpdates$ = this.scoreUpdates.asObservable();

  // ---- identity ---------------------------------------------------------------------------

  register(email: string, fullName: string, password: string): DemoUser {
    const normalised = email.trim().toLowerCase();
    if (this.state.users.some(u => u.email === normalised)) {
      throw new DemoFailure(409, 'Conflict', 'An account with this e-mail address already exists.', 'User.EmailTaken');
    }
    const user: DemoUser = { id: crypto.randomUUID(), email: normalised, fullName: fullName.trim(), password };
    this.state.users.push(user);
    this.openWallet(user, new Date());
    this.save();
    return user;
  }

  login(email: string, password: string): DemoUser {
    const user = this.state.users.find(u => u.email === email.trim().toLowerCase());
    if (!user || user.password !== password) {
      throw new DemoFailure(401, 'Unauthorized', 'The e-mail or password is incorrect.', 'User.InvalidCredentials');
    }
    return user;
  }

  // ---- wallet -----------------------------------------------------------------------------

  summary(userId: string): WalletSummary {
    const user = this.user(userId);
    const wallet = this.state.wallets[userId];
    const credits = wallet.entries.filter(e => e.direction === 'Credit').reduce((s, e) => s + e.amount, 0);
    const debits = wallet.entries.filter(e => e.direction === 'Debit').reduce((s, e) => s + e.amount, 0);
    return {
      walletId: wallet.walletId,
      ownerName: user.fullName,
      ownerEmail: user.email,
      currency: 'TRY',
      balance: round(wallet.balance),
      totalInflow: round(credits),
      totalOutflow: round(debits),
      entryCount: wallet.entries.length,
      openedAtUtc: wallet.openedAtUtc,
      lastActivityAtUtc: wallet.entries[0]?.postedAtUtc ?? null
    };
  }

  ledger(userId: string, page: number, pageSize: number) {
    const entries = this.state.wallets[userId].entries;
    return {
      items: entries.slice((page - 1) * pageSize, page * pageSize),
      page,
      pageSize,
      totalCount: entries.length,
      totalPages: Math.ceil(entries.length / pageSize)
    };
  }

  deposit(userId: string, amount: number, memo?: string): TransactionReceipt {
    this.assertAmount(amount);
    const entry = this.post(userId, 'Deposit', amount, memo || 'Cash deposit', null, new Date());
    this.save();
    return this.receipt(entry);
  }

  withdraw(userId: string, amount: number, memo?: string): TransactionReceipt {
    this.assertAmount(amount);
    this.assertFunds(userId, amount);
    const entry = this.post(userId, 'Withdrawal', amount, memo || 'Cash withdrawal', null, new Date());
    this.save();
    return this.receipt(entry);
  }

  transfer(userId: string, recipientEmail: string, amount: number, memo?: string): TransactionReceipt {
    this.assertAmount(amount);
    const sender = this.user(userId);
    const recipient = this.state.users.find(u => u.email === recipientEmail.trim().toLowerCase());
    if (!recipient) {
      throw new DemoFailure(404, 'Resource not found', 'No wallet is registered for the recipient e-mail.', 'Wallet.RecipientNotFound');
    }
    if (recipient.id === userId) {
      throw new DemoFailure(422, 'Business rule violated', 'You cannot send money to your own wallet.', 'Wallet.SelfTransfer');
    }
    this.assertFunds(userId, amount);
    const now = new Date();
    const out = this.post(userId, 'TransferOut', amount, memo || `Transfer to ${recipient.fullName}`, recipient.fullName, now);
    this.post(recipient.id, 'TransferIn', amount, memo || `Transfer from ${sender.fullName}`, sender.fullName, now);
    this.save();
    return this.receipt(out);
  }

  // ---- scoring ----------------------------------------------------------------------------

  report(userId: string): CreditReport {
    const p = this.state.profiles[userId];
    const { factors, band } = evaluate(p);
    const last = p.history[p.history.length - 1];
    return {
      userId,
      displayName: p.displayName,
      score: p.score,
      band,
      minScore: MIN_SCORE,
      maxScore: MAX_SCORE,
      lastDelta: last?.delta ?? 0,
      lastReason: last?.reason ?? null,
      factors,
      history: [...p.history],
      stats: {
        totalInflow: round(p.totalInflow),
        totalOutflow: round(p.totalOutflow),
        lastKnownBalance: round(p.lastKnownBalance),
        peakBalance: round(p.peakBalance),
        transactionCount: p.transactionCount,
        declinedCount: p.declinedCount,
        drainEventCount: p.drainEventCount
      },
      updatedAtUtc: p.updatedAtUtc
    };
  }

  // ---- internals --------------------------------------------------------------------------

  private post(userId: string, type: LedgerEntryType, amount: number, description: string, counterparty: string | null, at: Date, async = true): LedgerEntry {
    const wallet = this.state.wallets[userId];
    const credit = type === 'Deposit' || type === 'TransferIn';
    wallet.balance = round(wallet.balance + (credit ? amount : -amount));

    const entry: LedgerEntry = {
      id: crypto.randomUUID(),
      type,
      direction: credit ? 'Credit' : 'Debit',
      amount: round(amount),
      balanceAfter: wallet.balance,
      description,
      reference: reference(type),
      counterparty,
      postedAtUtc: at.toISOString()
    };
    wallet.entries.unshift(entry);

    const signal = { kind: type, amount, balanceAfter: wallet.balance, at };
    if (async) {
      // Wallet → outbox → RabbitMQ → Scoring → SignalR takes a beat; emulate it.
      setTimeout(() => this.applySignal(userId, signal, true), 650);
    } else {
      this.applySignal(userId, signal, false);
    }
    return entry;
  }

  private applySignal(userId: string, s: { kind: LedgerEntryType | 'Declined'; amount: number; balanceAfter: number; at: Date }, emit: boolean): void {
    const p = this.state.profiles[userId];
    if (s.kind === 'Deposit' || s.kind === 'TransferIn') {
      p.totalInflow += s.amount;
      p.transactionCount++;
    } else if (s.kind === 'Withdrawal' || s.kind === 'TransferOut') {
      const before = s.balanceAfter + s.amount;
      if (before > 0 && s.amount > before * DRAIN_THRESHOLD) p.drainEventCount++;
      p.totalOutflow += s.amount;
      p.transactionCount++;
    } else {
      p.declinedCount++;
    }
    p.lastKnownBalance = s.balanceAfter;
    p.peakBalance = Math.max(p.peakBalance, s.balanceAfter);
    p.updatedAtUtc = s.at.toISOString();

    const { score } = evaluate(p);
    const delta = score - p.score;
    p.score = score;
    p.history.push({ score, delta, reason: describe(s.kind, s.amount), recordedAtUtc: s.at.toISOString() });
    if (p.history.length > 60) p.history.shift();

    this.save();
    if (emit) this.scoreUpdates.next(this.report(userId));
  }

  private assertAmount(amount: number): void {
    if (!(amount > 0)) {
      throw new DemoFailure(400, 'Validation failed', 'Amount must be greater than zero.', 'Wallet.AmountMustBePositive');
    }
    if (amount > 250_000) {
      throw new DemoFailure(422, 'Business rule violated', 'A single transaction cannot exceed 250,000.', 'Wallet.AmountAboveLimit');
    }
  }

  private assertFunds(userId: string, amount: number): void {
    const wallet = this.state.wallets[userId];
    if (wallet.balance < amount) {
      setTimeout(() => this.applySignal(userId, { kind: 'Declined', amount, balanceAfter: wallet.balance, at: new Date() }, true), 650);
      throw new DemoFailure(422, 'Business rule violated', 'The wallet balance is too low for this transaction.', 'Wallet.InsufficientFunds');
    }
  }

  private receipt(entry: LedgerEntry): TransactionReceipt {
    return { entryId: entry.id, reference: entry.reference, type: entry.type, amount: entry.amount, balanceAfter: entry.balanceAfter, currency: 'TRY', postedAtUtc: entry.postedAtUtc };
  }

  private user(userId: string): DemoUser {
    const user = this.state.users.find(u => u.id === userId);
    if (!user) throw new DemoFailure(401, 'Unauthorized', 'Unknown session.', 'User.NotFound');
    return user;
  }

  private openWallet(user: DemoUser, at: Date): void {
    this.state.wallets[user.id] = { walletId: crypto.randomUUID(), balance: 0, openedAtUtc: at.toISOString(), entries: [] };
    const base = { totalInflow: 0, totalOutflow: 0, lastKnownBalance: 0, peakBalance: 0, transactionCount: 0, declinedCount: 0, drainEventCount: 0 };
    const { score } = evaluate(base);
    this.state.profiles[user.id] = {
      ...base,
      displayName: user.fullName,
      score,
      history: [{ score, delta: 0, reason: 'Credit file opened', recordedAtUtc: at.toISOString() }],
      updatedAtUtc: at.toISOString()
    };
  }

  private load(): DemoState {
    try {
      const raw = sessionStorage.getItem(STORAGE_KEY);
      if (raw) return JSON.parse(raw) as DemoState;
    } catch {
      /* fall through to seed */
    }
    return this.seed();
  }

  private save(): void {
    try {
      sessionStorage.setItem(STORAGE_KEY, JSON.stringify(this.state));
    } catch {
      /* in-memory only */
    }
  }

  /** A believable first week for the demo customer, replayed through the same rules. */
  private seed(): DemoState {
    this.state = { users: [], wallets: {}, profiles: {} };
    const day = 86_400_000;
    const now = Date.now();
    const at = (daysAgo: number, hour: number, minute = 0) => {
      const d = new Date(now - daysAgo * day);
      d.setHours(hour, minute, 0, 0);
      return d;
    };

    const ada: DemoUser = { id: crypto.randomUUID(), email: DEMO_ACCOUNT.email, fullName: 'Ada Lovelace', password: DEMO_ACCOUNT.password };
    const grace: DemoUser = { id: crypto.randomUUID(), email: 'grace@tessera.dev', fullName: 'Grace Hopper', password: 'Ledger2026' };
    const alan: DemoUser = { id: crypto.randomUUID(), email: 'alan@tessera.dev', fullName: 'Alan Turing', password: 'Ledger2026' };
    this.state.users.push(ada, grace, alan);
    [ada, grace, alan].forEach(u => this.openWallet(u, at(12, 9)));

    this.post(grace.id, 'Deposit', 15_000, 'Opening float', null, at(11, 10), false);
    this.post(alan.id, 'Deposit', 9_000, 'Opening float', null, at(11, 10), false);

    const script: Array<[LedgerEntryType | 'Declined', number, string, number, number, (string | null)?]> = [
      ['Deposit', 8_500, 'Salary — Analytical Engines Ltd.', 9, 9],
      ['Withdrawal', 320.75, 'Groceries, Kadıköy market', 8, 18],
      ['TransferOut', 450, 'Concert tickets', 7, 20, 'Grace Hopper'],
      ['Deposit', 1_200, 'Freelance invoice #114', 6, 11],
      ['Withdrawal', 2_000, 'Rent share', 5, 9],
      ['Declined', 12_000, '', 5, 15],
      ['TransferIn', 260, 'Lunch split', 4, 13, 'Alan Turing'],
      ['Deposit', 3_400, 'Quarterly bonus', 3, 10],
      ['Withdrawal', 180.4, 'Pharmacy', 2, 17],
      ['TransferOut', 75, 'Second-hand books', 1, 12, 'Alan Turing'],
      ['Deposit', 950, 'Side project payout', 0, 8, null]
    ];

    for (const [kind, amount, memo, daysAgo, hour, counterparty] of script) {
      const when = at(daysAgo, hour, 15);
      if (kind === 'Declined') {
        this.applySignal(ada.id, { kind, amount, balanceAfter: this.state.wallets[ada.id].balance, at: when }, false);
      } else {
        this.post(ada.id, kind, amount, memo, counterparty ?? null, when, false);
      }
    }

    this.save();
    return this.state;
  }
}

function round(n: number): number {
  return Math.round(n * 100) / 100;
}

function reference(type: LedgerEntryType): string {
  const alphabet = 'ABCDEFGHJKLMNPQRSTUVWXYZ23456789';
  const prefix = { Deposit: 'DEP', Withdrawal: 'WDL', TransferIn: 'TRI', TransferOut: 'TRO' }[type];
  let code = '';
  for (let i = 0; i < 8; i++) code += alphabet[Math.floor(Math.random() * alphabet.length)];
  return `${prefix}-${code}`;
}

function describe(kind: LedgerEntryType | 'Declined', amount: number): string {
  const formatted = amount.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
  switch (kind) {
    case 'Deposit': return `Deposit of ${formatted}`;
    case 'Withdrawal': return `Withdrawal of ${formatted}`;
    case 'TransferIn': return `Incoming transfer of ${formatted}`;
    case 'TransferOut': return `Outgoing transfer of ${formatted}`;
    default: return `Declined payment of ${formatted}`;
  }
}
