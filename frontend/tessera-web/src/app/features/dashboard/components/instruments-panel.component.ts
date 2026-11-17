import { ChangeDetectionStrategy, Component, inject, input, output, signal } from '@angular/core';
import { AbstractControl, FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Observable } from 'rxjs';
import { isApiError } from '../../../core/http/error.interceptor';
import { ApiError, InstrumentKind, TransactionReceipt } from '../../../core/models';
import { ToastService } from '../../../core/notify/toast.service';
import { AmountPipe } from '../../../shared/formatting';
import { InstrumentRequest } from '../dashboard.store';

interface InstrumentTab {
  kind: InstrumentKind;
  label: string;
  verb: string;
  hint: string;
}

const TWO_DECIMALS = /^\d+(\.\d{1,2})?$/;

@Component({
  selector: 'ts-instruments-panel',
  standalone: true,
  imports: [ReactiveFormsModule, AmountPipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <h2 class="kicker"><span class="section-mark mr-2 text-base normal-case tracking-normal">§3</span>Instruments</h2>

    <!-- Index-card tabs -->
    <div class="mt-5 flex items-end gap-1" role="tablist">
      @for (tab of tabs; track tab.kind) {
        <button type="button" role="tab" [attr.aria-selected]="kind() === tab.kind"
                class="relative -mb-[2px] border-2 px-4 py-2 font-display text-[17px] transition-colors"
                [class.border-ink]="kind() === tab.kind" [class.border-b-paper]="kind() === tab.kind" [class.bg-paper]="kind() === tab.kind"
                [class.z-10]="kind() === tab.kind" [class.font-semibold]="kind() === tab.kind"
                [class.border-transparent]="kind() !== tab.kind" [class.italic]="kind() !== tab.kind" [class.text-ink-soft]="kind() !== tab.kind"
                (click)="select(tab.kind)">
          {{ tab.label }}
        </button>
      }
    </div>

    <form class="border-2 border-ink bg-paper px-5 pb-6 pt-5" [formGroup]="form" (ngSubmit)="submit()" novalidate>
      <p class="font-display text-[15px] italic text-ink-soft">{{ active().hint }}</p>

      <div class="mt-5">
        <label class="field-label" for="amount">Amount</label>
        <div class="flex items-baseline gap-2 border-b-2 border-ink" [style.border-color]="invalid(form.controls.amount) ? '#C8361D' : null">
          <span class="font-mono text-xl text-ink-soft">₺</span>
          <input id="amount" inputmode="decimal" autocomplete="off" formControlName="amount" placeholder="0.00"
                 class="w-full border-0 bg-transparent px-0 py-2 font-mono text-[34px] leading-none tracking-tight figures focus:outline-none focus:ring-0">
        </div>
        @if (invalid(form.controls.amount)) {
          <p class="field-error">{{ amountError() }}</p>
        }
        <div class="mt-3 flex flex-wrap gap-2">
          @for (preset of presets; track preset) {
            <button type="button" class="chip" (click)="setAmount(preset)">{{ preset | amount }}</button>
          }
        </div>
      </div>

      @if (kind() === 'send') {
        <div class="mt-5">
          <label class="field-label" for="recipient">Recipient e-mail</label>
          <input id="recipient" type="email" class="line-input" formControlName="recipientEmail" placeholder="grace@tessera.dev" list="known-recipients">
          <datalist id="known-recipients">
            @for (r of knownRecipients(); track r) { <option [value]="r"></option> }
          </datalist>
          @if (invalid(form.controls.recipientEmail)) {
            <p class="field-error">A valid recipient e-mail is required.</p>
          }
        </div>
      }

      <div class="mt-5">
        <label class="field-label" for="memo">{{ kind() === 'send' ? 'Note to recipient' : 'Particulars' }} <span class="normal-case tracking-normal text-ink-faint">(optional)</span></label>
        <input id="memo" class="line-input" formControlName="memo" maxlength="140" [placeholder]="memoPlaceholder()">
      </div>

      @if (refusal(); as error) {
        <div class="mt-5 border-l-4 border-vermilion bg-vermilion-soft/45 px-4 py-3" role="alert">
          <p class="font-mono text-[11px] font-semibold uppercase tracking-[0.2em] text-vermilion">✕ Refused · {{ error.code }}</p>
          <p class="mt-1 text-sm">{{ error.detail }}</p>
          @if (error.code === 'Wallet.InsufficientFunds') {
            <p class="mt-1 font-display text-sm italic text-ink-soft">The attempt was reported to the scoring desk — watch the dial.</p>
          }
        </div>
      }

      <button type="submit" class="stamp-btn mt-6 w-full" [disabled]="busy()">
        @if (busy()) { Posting… } @else { {{ active().verb }} <span aria-hidden="true">↵</span> }
      </button>
    </form>

    @if (receipt(); as r) {
      <div class="relative mt-5 animate-slip-in border-2 border-dashed border-ink/70 bg-paper px-5 py-4">
        <div class="flex items-start justify-between gap-4">
          <div>
            <p class="kicker">Receipt</p>
            <p class="mt-1 font-mono text-sm font-semibold">{{ r.reference }}</p>
            <p class="mt-2 font-mono text-[12px] text-ink-soft">
              {{ r.type }} · {{ r.type === 'Deposit' || r.type === 'TransferIn' ? '+' : '−' }}{{ r.amount | amount }} {{ r.currency }}<br>
              Balance after: {{ r.balanceAfter | amount }}
            </p>
          </div>
          <span class="animate-stamp-in rounded-sm border-[3px] border-verdigris px-2 py-1 font-mono text-sm font-bold uppercase tracking-[0.25em] text-verdigris">Posted</span>
        </div>
      </div>
    }
  `
})
export class InstrumentsPanelComponent {
  private readonly fb = inject(FormBuilder);
  private readonly toasts = inject(ToastService);

  /** Executes the request and returns the receipt (wired to DashboardStore.execute). */
  readonly execute = input.required<(request: InstrumentRequest) => Observable<TransactionReceipt>>();
  readonly knownRecipients = input<string[]>([]);
  readonly posted = output<TransactionReceipt>();

  protected readonly tabs: InstrumentTab[] = [
    { kind: 'deposit', label: 'Deposit', verb: 'Post deposit', hint: 'Credit cash into the wallet. Healthy inflows lift the score.' },
    { kind: 'withdraw', label: 'Withdraw', verb: 'Post withdrawal', hint: 'Debit cash out. Draining more than 60% at once counts as volatility.' },
    { kind: 'send', label: 'Send', verb: 'Send transfer', hint: 'Move money to another Tessera wallet. Both legs post atomically.' }
  ];

  protected readonly presets = [100, 500, 1_000, 5_000];
  protected readonly kind = signal<InstrumentKind>('deposit');
  protected readonly busy = signal(false);
  protected readonly refusal = signal<ApiError | null>(null);
  protected readonly receipt = signal<TransactionReceipt | null>(null);

  protected readonly form = this.fb.nonNullable.group({
    amount: ['', [Validators.required, Validators.pattern(TWO_DECIMALS), Validators.min(0.01), Validators.max(250_000)]],
    recipientEmail: [''],
    memo: ['', [Validators.maxLength(140)]]
  });

  protected active(): InstrumentTab {
    return this.tabs.find(t => t.kind === this.kind())!;
  }

  protected memoPlaceholder(): string {
    return { deposit: 'e.g. Salary — November', withdraw: 'e.g. Rent share', send: 'e.g. Dinner at Karaköy' }[this.kind()];
  }

  protected select(kind: InstrumentKind): void {
    this.kind.set(kind);
    this.refusal.set(null);
    const recipient = this.form.controls.recipientEmail;
    recipient.setValidators(kind === 'send' ? [Validators.required, Validators.email] : []);
    recipient.updateValueAndValidity();
  }

  protected setAmount(value: number): void {
    this.form.controls.amount.setValue(value.toFixed(2));
    this.form.controls.amount.markAsTouched();
  }

  protected submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const { amount, recipientEmail, memo } = this.form.getRawValue();
    this.busy.set(true);
    this.refusal.set(null);

    this.execute()({
      kind: this.kind(),
      amount: Number(amount),
      memo: memo.trim() || undefined,
      recipientEmail: recipientEmail.trim() || undefined
    }).subscribe({
      next: receipt => {
        this.busy.set(false);
        this.receipt.set(receipt);
        this.form.reset({ amount: '', recipientEmail: this.kind() === 'send' ? recipientEmail : '', memo: '' });
        this.toasts.push('success', `${receipt.reference} posted`, 'Ledger updated. The scoring desk is re-rating your file…', 3800);
        this.posted.emit(receipt);
      },
      error: (error: unknown) => {
        this.busy.set(false);
        if (isApiError(error) && error.status >= 400 && error.status < 500) {
          this.refusal.set(error);
          for (const [field, messages] of Object.entries(error.fieldErrors)) {
            this.form.get(field)?.setErrors({ server: messages[0] });
          }
        }
      }
    });
  }

  protected invalid(control: AbstractControl): boolean {
    return control.invalid && control.touched;
  }

  protected amountError(): string {
    const e = this.form.controls.amount.errors ?? {};
    if (e['server']) return e['server'];
    if (e['required']) return 'Enter an amount.';
    if (e['pattern']) return 'Use a positive number with up to two decimals.';
    if (e['min']) return 'Amount must be greater than zero.';
    if (e['max']) return 'A single transaction cannot exceed 250,000.00.';
    return 'Invalid amount.';
  }
}
