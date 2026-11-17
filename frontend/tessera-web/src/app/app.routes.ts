import { Routes } from '@angular/router';
import { authenticatedGuard, guestGuard } from './core/auth/guards';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'ledger' },
  {
    path: 'sign-in',
    canMatch: [guestGuard],
    title: 'Sign in · Tessera',
    loadComponent: () => import('./features/auth/auth-page.component').then(m => m.AuthPageComponent)
  },
  {
    path: 'ledger',
    canMatch: [authenticatedGuard],
    title: 'Ledger · Tessera',
    loadComponent: () => import('./features/dashboard/dashboard-page.component').then(m => m.DashboardPageComponent)
  },
  { path: '**', redirectTo: 'ledger' }
];
