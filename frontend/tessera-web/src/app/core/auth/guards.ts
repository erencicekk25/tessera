import { inject } from '@angular/core';
import { CanMatchFn, Router } from '@angular/router';
import { SessionStore } from './session.store';

export const authenticatedGuard: CanMatchFn = () =>
  inject(SessionStore).isAuthenticated() || inject(Router).createUrlTree(['/sign-in']);

export const guestGuard: CanMatchFn = () =>
  !inject(SessionStore).isAuthenticated() || inject(Router).createUrlTree(['/ledger']);
