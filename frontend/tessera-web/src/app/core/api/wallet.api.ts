import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../config';
import { CreditReport, LedgerEntry, PagedResult, TransactionReceipt, WalletSummary } from '../models';

@Injectable({ providedIn: 'root' })
export class WalletApi {
  private readonly http = inject(HttpClient);
  private readonly url = `${inject(API_BASE_URL)}/api/wallet`;

  summary(): Observable<WalletSummary> {
    return this.http.get<WalletSummary>(this.url);
  }

  ledger(page: number, pageSize: number): Observable<PagedResult<LedgerEntry>> {
    const params = new HttpParams().set('page', page).set('pageSize', pageSize);
    return this.http.get<PagedResult<LedgerEntry>>(`${this.url}/ledger`, { params });
  }

  deposit(amount: number, description?: string): Observable<TransactionReceipt> {
    return this.http.post<TransactionReceipt>(`${this.url}/deposits`, { amount, description });
  }

  withdraw(amount: number, description?: string): Observable<TransactionReceipt> {
    return this.http.post<TransactionReceipt>(`${this.url}/withdrawals`, { amount, description });
  }

  transfer(recipientEmail: string, amount: number, note?: string): Observable<TransactionReceipt> {
    return this.http.post<TransactionReceipt>(`${this.url}/transfers`, { recipientEmail, amount, note });
  }
}

@Injectable({ providedIn: 'root' })
export class ScoreApi {
  private readonly http = inject(HttpClient);
  private readonly url = `${inject(API_BASE_URL)}/api/scores`;

  mine(): Observable<CreditReport> {
    return this.http.get<CreditReport>(`${this.url}/me`);
  }
}
