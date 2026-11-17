import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { AuthService } from '../auth/auth.service';
import { ApiError } from '../models';
import { ToastService } from '../notify/toast.service';

/**
 * Normalises every HTTP failure into an {@link ApiError} and applies cross-cutting policies:
 *  - 401 on a protected call  → session is over: sign out and return to the sign-in page
 *  - 0 / 5xx / 429            → global toast (forms still receive the error to reset their state)
 *  - 400 / 404 / 409 / 422    → left to the calling form to render inline
 */
export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const toasts = inject(ToastService);

  return next(req).pipe(
    catchError((response: unknown) => {
      if (!(response instanceof HttpErrorResponse)) {
        return throwError(() => response);
      }

      const error = toApiError(response);
      const isAuthEndpoint = req.url.includes('/api/auth/');

      if (error.status === 401 && !isAuthEndpoint) {
        toasts.push('warn', 'Session closed', 'Your access token expired. Please sign in again.');
        auth.signOut('expired');
      } else if (error.status === 0) {
        toasts.push('error', 'Line is down', 'The gateway could not be reached. Is the backend running?');
      } else if (error.status === 429) {
        toasts.push('warn', 'Slow down', 'Too many attempts — try again in a minute.');
      } else if (error.status >= 500) {
        toasts.push('error', error.title, error.detail);
      }

      return throwError(() => error);
    })
  );
};

function toApiError(response: HttpErrorResponse): ApiError {
  const body = (typeof response.error === 'object' && response.error) || {};
  const problem = body as Partial<{ title: string; detail: string; code: string; errors: Record<string, string[]> }>;

  if (response.status === 0) {
    return { status: 0, code: 'Network', title: 'Network error', detail: 'The server is unreachable.', fieldErrors: {} };
  }

  return {
    status: response.status,
    code: problem.code ?? null,
    title: problem.title ?? response.statusText ?? 'Request failed',
    detail: problem.detail ?? problem.title ?? 'Something went wrong.',
    fieldErrors: problem.errors ?? {}
  };
}

export function isApiError(value: unknown): value is ApiError {
  return typeof value === 'object' && value !== null && 'status' in value && 'fieldErrors' in value;
}
