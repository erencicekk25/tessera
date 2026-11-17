/** Wire contracts — mirror the DTOs exposed through Gateway.Api. */

export interface UserDto {
  id: string;
  email: string;
  fullName: string;
}

export interface AuthResponse {
  accessToken: string;
  expiresAtUtc: string;
  user: UserDto;
}

export interface WalletSummary {
  walletId: string;
  ownerName: string;
  ownerEmail: string;
  currency: string;
  balance: number;
  totalInflow: number;
  totalOutflow: number;
  entryCount: number;
  openedAtUtc: string;
  lastActivityAtUtc: string | null;
}

export type LedgerEntryType = 'Deposit' | 'Withdrawal' | 'TransferIn' | 'TransferOut';

export interface LedgerEntry {
  id: string;
  type: LedgerEntryType;
  direction: 'Credit' | 'Debit';
  amount: number;
  balanceAfter: number;
  description: string;
  reference: string;
  counterparty: string | null;
  postedAtUtc: string;
}

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export interface TransactionReceipt {
  entryId: string;
  reference: string;
  type: LedgerEntryType;
  amount: number;
  balanceAfter: number;
  currency: string;
  postedAtUtc: string;
}

export type ScoreBand = 'Poor' | 'Fair' | 'Good' | 'VeryGood' | 'Exceptional';

export interface ScoreFactor {
  key: string;
  label: string;
  points: number;
  minPoints: number;
  maxPoints: number;
  impact: 'positive' | 'negative' | 'neutral';
  explanation: string;
}

export interface ScoreSnapshot {
  score: number;
  delta: number;
  reason: string;
  recordedAtUtc: string;
}

export interface CreditStats {
  totalInflow: number;
  totalOutflow: number;
  lastKnownBalance: number;
  peakBalance: number;
  transactionCount: number;
  declinedCount: number;
  drainEventCount: number;
}

export interface CreditReport {
  userId: string;
  displayName: string;
  score: number;
  band: ScoreBand;
  minScore: number;
  maxScore: number;
  lastDelta: number;
  lastReason: string | null;
  factors: ScoreFactor[];
  history: ScoreSnapshot[];
  stats: CreditStats;
  updatedAtUtc: string;
}

export type InstrumentKind = 'deposit' | 'withdraw' | 'send';

/** Normalised error produced by the HTTP error interceptor from RFC 9457 problem details. */
export interface ApiError {
  status: number;
  code: string | null;
  title: string;
  detail: string;
  fieldErrors: Record<string, string[]>;
}
