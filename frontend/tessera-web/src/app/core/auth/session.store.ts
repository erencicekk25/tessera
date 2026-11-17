import { Injectable, computed, signal } from '@angular/core';
import { AuthResponse, UserDto } from '../models';

interface Session {
  accessToken: string;
  expiresAt: number;
  user: UserDto;
}

const STORAGE_KEY = 'tessera.session';

/**
 * Single source of truth for the signed-in user. Backed by signals; persisted to localStorage so a
 * refresh keeps the session until the JWT itself expires.
 */
@Injectable({ providedIn: 'root' })
export class SessionStore {
  private readonly session = signal<Session | null>(this.restore());
  private expiryTimer?: ReturnType<typeof setTimeout>;

  readonly user = computed(() => this.session()?.user ?? null);
  readonly accessToken = computed(() => this.session()?.accessToken ?? null);
  readonly isAuthenticated = computed(() => {
    const s = this.session();
    return !!s && s.expiresAt > Date.now();
  });

  constructor() {
    this.scheduleExpiry(this.session());
  }

  start(response: AuthResponse): void {
    const session: Session = {
      accessToken: response.accessToken,
      expiresAt: Date.parse(response.expiresAtUtc),
      user: response.user
    };
    this.session.set(session);
    this.persist(session);
    this.scheduleExpiry(session);
  }

  clear(): void {
    this.session.set(null);
    this.persist(null);
    clearTimeout(this.expiryTimer);
  }

  private scheduleExpiry(session: Session | null): void {
    clearTimeout(this.expiryTimer);
    if (!session) return;
    const remaining = session.expiresAt - Date.now();
    // setTimeout overflows above ~24.8 days; tokens here live for hours.
    if (remaining > 0 && remaining < 2_000_000_000) {
      this.expiryTimer = setTimeout(() => this.clear(), remaining);
    }
  }

  private restore(): Session | null {
    try {
      const raw = localStorage.getItem(STORAGE_KEY);
      if (!raw) return null;
      const parsed = JSON.parse(raw) as Session;
      return parsed.expiresAt > Date.now() ? parsed : null;
    } catch {
      return null;
    }
  }

  private persist(session: Session | null): void {
    try {
      if (session) localStorage.setItem(STORAGE_KEY, JSON.stringify(session));
      else localStorage.removeItem(STORAGE_KEY);
    } catch {
      /* storage unavailable (private mode) — session stays in memory */
    }
  }
}
