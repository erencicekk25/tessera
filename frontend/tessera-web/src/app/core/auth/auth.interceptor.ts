import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { API_BASE_URL } from '../config';
import { SessionStore } from './session.store';

/**
 * Attaches the bearer token to calls aimed at our own API only — never leak the JWT
 * to third-party origins (fonts, CDNs, ...).
 */
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const token = inject(SessionStore).accessToken();
  const baseUrl = inject(API_BASE_URL);

  const isApiCall = baseUrl ? req.url.startsWith(baseUrl) : req.url.startsWith('/api');
  const isAuthEndpoint = req.url.includes('/api/auth/');

  if (!token || !isApiCall || isAuthEndpoint) {
    return next(req);
  }

  return next(req.clone({ setHeaders: { Authorization: `Bearer ${token}` } }));
};
