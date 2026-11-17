import { ChangeDetectionStrategy, Component, OnInit, inject, input, signal } from '@angular/core';
import { AbstractControl, FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';
import { IS_DEMO } from '../../core/config';
import { DEMO_ACCOUNT } from '../../core/demo/demo-backend';
import { isApiError } from '../../core/http/error.interceptor';

type Mode = 'sign-in' | 'sign-up';

@Component({
  selector: 'ts-auth-page',
  standalone: true,
  imports: [ReactiveFormsModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="min-h-screen lg:grid lg:grid-cols-[1.2fr_1fr]">
      <!-- Editorial side -->
      <section class="relative flex flex-col justify-between border-b-2 border-ink px-5 py-8 sm:px-10 lg:border-b-0 lg:border-r-2 lg:px-14 lg:py-12">
        <header class="flex items-center justify-between">
          <span class="font-display text-2xl font-semibold tracking-tight">Tessera<span class="text-vermilion">.</span></span>
          <span class="kicker">Est. 2026 · Istanbul · A simulation</span>
        </header>

        <div class="my-12 lg:my-0">
          <p class="kicker mb-6">The personal ledger &amp; credit gazette</p>
          <h1 class="font-display text-[clamp(52px,8.4vw,132px)] font-light leading-[0.86] tracking-[-0.035em]" style="font-variation-settings:'opsz' 144">
            Every lira,<br><em class="font-normal">accounted</em><br>for.
          </h1>
          <p class="mt-8 max-w-xl text-[17px] leading-relaxed text-ink-soft">
            A double-entry wallet whose credit score re-prices itself the moment money moves.
            Deposits, withdrawals and transfers are posted to an append-only ledger; the scoring desk
            hears about every one of them over the wire.
          </p>
        </div>

        <div class="grid gap-6 rule-thick pt-5 sm:grid-cols-3">
          @for (article of articles; track article.numeral) {
            <article>
              <p class="font-display text-3xl italic text-vermilion">{{ article.numeral }}</p>
              <h2 class="mt-1 font-display text-lg font-semibold">{{ article.title }}</h2>
              <p class="mt-1 text-sm leading-snug text-ink-soft">{{ article.body }}</p>
            </article>
          }
        </div>

        <div class="pointer-events-none absolute right-8 top-28 hidden h-36 w-36 -rotate-12 select-none items-center justify-center rounded-full border-[3px] border-vermilion/70 text-center xl:flex">
          <span class="font-mono text-[10px] font-semibold uppercase leading-relaxed tracking-[0.25em] text-vermilion/80">Specimen<br>— not —<br>legal tender</span>
        </div>
      </section>

      <!-- Form side -->
      <section class="flex items-center justify-center bg-paper-deep/60 px-5 py-12 sm:px-10">
        <div class="w-full max-w-md">
          <div class="border-2 border-ink bg-paper shadow-slip">
            <div class="flex items-center justify-between border-b-2 border-ink px-6 py-3">
              <span class="kicker">Form T-1 · Account access</span>
              <span class="font-mono text-[10px] text-ink-faint">No. {{ formNumber }}</span>
            </div>

            <div class="grid grid-cols-2 border-b border-ink/30" role="tablist">
              @for (option of modes; track option.value) {
                <button type="button" role="tab" [attr.aria-selected]="mode() === option.value"
                        class="px-4 py-3 font-display text-lg transition-colors"
                        [class.bg-ink]="mode() === option.value" [class.text-paper]="mode() === option.value"
                        [class.italic]="mode() !== option.value" [class.text-ink-soft]="mode() !== option.value"
                        (click)="switchMode(option.value)">
                  {{ option.label }}
                </button>
              }
            </div>

            <form class="space-y-6 px-6 pb-7 pt-6" [formGroup]="form" (ngSubmit)="submit()" novalidate>
              @if (reason() === 'expired') {
                <p class="border border-dashed border-ochre bg-ochre-soft/50 px-3 py-2 font-mono text-[12px]">Your session expired. Sign in again to continue.</p>
              }

              @if (mode() === 'sign-up') {
                <div>
                  <label class="field-label" for="fullName">Full name of account holder</label>
                  <input id="fullName" class="line-input" formControlName="fullName" autocomplete="name" placeholder="Ada Lovelace">
                  @if (showError(form.controls.fullName)) {
                    <p class="field-error">{{ errorText(form.controls.fullName) }}</p>
                  }
                </div>
              }

              <div>
                <label class="field-label" for="email">E-mail</label>
                <input id="email" type="email" class="line-input" formControlName="email" autocomplete="email" placeholder="you@example.com">
                @if (showError(form.controls.email)) {
                  <p class="field-error">{{ errorText(form.controls.email) }}</p>
                }
              </div>

              <div>
                <label class="field-label" for="password">Password</label>
                <input id="password" type="password" class="line-input" formControlName="password"
                       [attr.autocomplete]="mode() === 'sign-up' ? 'new-password' : 'current-password'" placeholder="••••••••">
                @if (showError(form.controls.password)) {
                  <p class="field-error">{{ errorText(form.controls.password) }}</p>
                } @else if (mode() === 'sign-up') {
                  <p class="mt-1 font-mono text-[11px] text-ink-faint">8+ characters, at least one letter and one digit.</p>
                }
              </div>

              @if (serverError()) {
                <p class="border-l-4 border-vermilion bg-vermilion-soft/50 px-3 py-2 text-sm" role="alert">{{ serverError() }}</p>
              }

              <button type="submit" class="stamp-btn w-full" [disabled]="busy()">
                @if (busy()) { Verifying… } @else { {{ mode() === 'sign-in' ? 'Open the ledger' : 'Open my account' }} <span aria-hidden="true">→</span> }
              </button>
            </form>
          </div>

          @if (isDemo) {
            <div class="mt-6 border border-dashed border-ink/50 px-5 py-4">
              <p class="kicker">Demo edition</p>
              <p class="mt-1 text-sm text-ink-soft">
                This build runs without a backend. Sign in as
                <span class="font-mono text-ink">{{ demoAccount.email }}</span> /
                <span class="font-mono text-ink">{{ demoAccount.password }}</span>.
              </p>
              <button type="button" class="ghost-btn mt-3" (click)="fillDemo()">Fill demo credentials</button>
            </div>
          }

          <p class="mt-6 text-center font-mono text-[10.5px] uppercase tracking-[0.2em] text-ink-faint">
            Secured with JWT bearer tokens · Gateway rate-limited
          </p>
        </div>
      </section>
    </div>
  `
})
export class AuthPageComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  /** Bound from the `?reason=` query parameter. */
  readonly reasonParam = input<string | undefined>(undefined, { alias: 'reason' });

  protected readonly isDemo = inject(IS_DEMO);
  protected readonly demoAccount = DEMO_ACCOUNT;
  protected readonly formNumber = String(Math.floor(Date.now() / 1000) % 100000).padStart(5, '0');
  protected readonly mode = signal<Mode>('sign-in');
  protected readonly busy = signal(false);
  protected readonly serverError = signal<string | null>(null);
  protected readonly reason = signal<string | undefined>(undefined);

  protected readonly modes: { value: Mode; label: string }[] = [
    { value: 'sign-in', label: 'Sign in' },
    { value: 'sign-up', label: 'Open an account' }
  ];

  protected readonly articles = [
    { numeral: 'I.', title: 'Ledger first', body: 'Balances are derived from immutable entries. Nothing is edited, only posted.' },
    { numeral: 'II.', title: 'Score in motion', body: 'Every posting is an event; the scoring desk re-rates you within a second.' },
    { numeral: 'III.', title: 'Nothing is lost', body: 'A transactional outbox guarantees each event leaves the building exactly when the money does.' }
  ];

  protected readonly form = this.fb.nonNullable.group({
    fullName: [''],
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required]]
  });

  ngOnInit(): void {
    this.reason.set(this.reasonParam());
  }

  protected switchMode(mode: Mode): void {
    this.mode.set(mode);
    this.serverError.set(null);
    const { fullName, password } = this.form.controls;

    if (mode === 'sign-up') {
      fullName.setValidators([Validators.required, Validators.minLength(2), Validators.maxLength(80)]);
      password.setValidators([Validators.required, Validators.minLength(8), Validators.pattern(/^(?=.*[A-Za-z])(?=.*\d).+$/)]);
    } else {
      fullName.clearValidators();
      password.setValidators([Validators.required]);
    }
    fullName.updateValueAndValidity();
    password.updateValueAndValidity();
  }

  protected fillDemo(): void {
    this.switchMode('sign-in');
    this.form.patchValue({ email: DEMO_ACCOUNT.email, password: DEMO_ACCOUNT.password });
  }

  protected submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.busy.set(true);
    this.serverError.set(null);
    const { email, password, fullName } = this.form.getRawValue();

    const request$ = this.mode() === 'sign-in'
      ? this.auth.signIn({ email, password })
      : this.auth.signUp({ email, password, fullName });

    request$.subscribe({
      next: () => void this.router.navigate(['/ledger']),
      error: (error: unknown) => {
        this.busy.set(false);
        if (!isApiError(error)) {
          this.serverError.set('Unexpected error. Please try again.');
          return;
        }
        // Map server-side validation errors onto the matching controls.
        for (const [field, messages] of Object.entries(error.fieldErrors)) {
          const control = this.form.get(field);
          control?.setErrors({ server: messages[0] });
          control?.markAsTouched();
        }
        if (error.status !== 0 && error.status < 500) {
          this.serverError.set(error.detail);
        }
      }
    });
  }

  protected showError(control: AbstractControl): boolean {
    return control.invalid && control.touched;
  }

  protected errorText(control: AbstractControl): string {
    const e = control.errors ?? {};
    if (e['server']) return e['server'];
    if (e['required']) return 'Required.';
    if (e['email']) return 'Enter a valid e-mail address.';
    if (e['minlength']) return `At least ${e['minlength'].requiredLength} characters.`;
    if (e['maxlength']) return `At most ${e['maxlength'].requiredLength} characters.`;
    if (e['pattern']) return 'Use at least one letter and one digit.';
    return 'Invalid value.';
  }
}
