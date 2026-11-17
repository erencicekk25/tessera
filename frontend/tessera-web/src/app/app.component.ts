import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { ToastOutletComponent } from './shared/toast-outlet.component';

@Component({
  selector: 'ts-root',
  standalone: true,
  imports: [RouterOutlet, ToastOutletComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <router-outlet />
    <ts-toast-outlet />
  `
})
export class AppComponent {}
