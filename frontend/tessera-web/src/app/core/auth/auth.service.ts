import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Router } from '@angular/router';
import { Observable, tap } from 'rxjs';
import { API_BASE_URL } from '../config';
import { AuthResponse } from '../models';
import { SessionStore } from './session.store';

export interface SignInRequest {
  email: string;
  password: string;
}

export interface SignUpRequest extends SignInRequest {
  fullName: string;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = inject(API_BASE_URL);
  private readonly session = inject(SessionStore);
  private readonly router = inject(Router);

  signIn(request: SignInRequest): Observable<AuthResponse> {
    return this.http
      .post<AuthResponse>(`${this.baseUrl}/api/auth/login`, request)
      .pipe(tap(response => this.session.start(response)));
  }

  signUp(request: SignUpRequest): Observable<AuthResponse> {
    return this.http
      .post<AuthResponse>(`${this.baseUrl}/api/auth/register`, request)
      .pipe(tap(response => this.session.start(response)));
  }

  signOut(reason?: 'expired'): void {
    this.session.clear();
    void this.router.navigate(['/sign-in'], { queryParams: reason ? { reason } : {} });
  }
}
