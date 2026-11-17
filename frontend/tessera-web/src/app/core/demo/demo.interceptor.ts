import { HttpErrorResponse, HttpInterceptorFn, HttpRequest, HttpResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { Observable, defer, delay, of, throwError } from 'rxjs';
import { DemoBackend, DemoFailure } from './demo-backend';

type Body = Record<string, unknown>;

/**
 * Demo mode only: answers gateway routes from {@link DemoBackend}. Registered *after* the auth
 * interceptor and *before* the error interceptor, so JWT handling and error mapping run exactly
 * as they do against the real backend.
 */
export const demoBackendInterceptor: HttpInterceptorFn = (req, next) => {
  const path = new URL(req.url, location.origin).pathname;
  if (!path.startsWith('/api/')) {
    return next(req);
  }

  const backend = inject(DemoBackend);
  return defer(() => route(backend, req, path)).pipe(delay(280));
};

function route(backend: DemoBackend, req: HttpRequest<unknown>, path: string): Observable<HttpResponse<unknown>> {
  try {
    const body = (req.body ?? {}) as Body;
    const key = `${req.method} ${path}`;

    if (key === 'POST /api/auth/login') {
      return ok(token(backend.login(String(body['email'] ?? ''), String(body['password'] ?? ''))));
    }
    if (key === 'POST /api/auth/register') {
      if (String(body['password'] ?? '').length < 8) {
        throw new DemoFailure(400, 'Validation failed', 'Password must be at least 8 characters long.', 'Request.Invalid');
      }
      return ok(token(backend.register(String(body['email']), String(body['fullName']), String(body['password']))), 201);
    }

    const userId = userFromBearer(req);

    switch (key) {
      case 'GET /api/wallet':
        return ok(backend.summary(userId));
      case 'GET /api/wallet/ledger':
        return ok(backend.ledger(userId, Number(req.params.get('page') ?? 1), Number(req.params.get('pageSize') ?? 10)));
      case 'POST /api/wallet/deposits':
        return ok(backend.deposit(userId, Number(body['amount']), body['description'] as string | undefined), 201);
      case 'POST /api/wallet/withdrawals':
        return ok(backend.withdraw(userId, Number(body['amount']), body['description'] as string | undefined), 201);
      case 'POST /api/wallet/transfers':
        return ok(backend.transfer(userId, String(body['recipientEmail']), Number(body['amount']), body['note'] as string | undefined), 201);
      case 'GET /api/scores/me':
        return ok(backend.report(userId));
      default:
        throw new DemoFailure(404, 'Resource not found', `No demo route for ${key}.`, 'Demo.NoRoute');
    }
  } catch (e) {
    const failure = e instanceof DemoFailure ? e : new DemoFailure(500, 'Unexpected error', String(e), 'Demo.Crash');
    return throwError(() => new HttpErrorResponse({
      status: failure.status,
      statusText: failure.title,
      url: req.url,
      error: { title: failure.title, detail: failure.detail, code: failure.code, status: failure.status }
    }));
  }
}

function ok(body: unknown, status = 200): Observable<HttpResponse<unknown>> {
  return of(new HttpResponse({ status, body }));
}

function token(user: { id: string; email: string; fullName: string }) {
  const payload = btoa(JSON.stringify({ sub: user.id, email: user.email, name: user.fullName }));
  return {
    accessToken: `demo.${payload}.unsigned`,
    expiresAtUtc: new Date(Date.now() + 2 * 3_600_000).toISOString(),
    user: { id: user.id, email: user.email, fullName: user.fullName }
  };
}

function userFromBearer(req: HttpRequest<unknown>): string {
  const header = req.headers.get('Authorization') ?? '';
  try {
    return JSON.parse(atob(header.replace('Bearer ', '').split('.')[1]))['sub'];
  } catch {
    throw new DemoFailure(401, 'Unauthorized', 'Missing or invalid bearer token.', 'Auth.Missing');
  }
}
