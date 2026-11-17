import { InjectionToken } from '@angular/core';
import { environment } from '../../environments/environment';

export const API_BASE_URL = new InjectionToken<string>('API_BASE_URL', {
  providedIn: 'root',
  factory: () => environment.apiBaseUrl
});

export const IS_DEMO = new InjectionToken<boolean>('IS_DEMO', {
  providedIn: 'root',
  factory: () => environment.demo
});
