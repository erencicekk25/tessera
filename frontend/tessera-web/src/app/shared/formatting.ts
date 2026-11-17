import { Pipe, PipeTransform } from '@angular/core';

const amountFormat = new Intl.NumberFormat('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 });

/** 12480.5 → "12,480.50" (ledger figures, always two decimals, grouping). */
@Pipe({ name: 'amount', standalone: true })
export class AmountPipe implements PipeTransform {
  transform(value: number | null | undefined): string {
    return value == null ? '—' : amountFormat.format(value);
  }
}

/** 12 → "+12", -7 → "−7" (true minus sign), 0 → "±0". */
@Pipe({ name: 'signed', standalone: true })
export class SignedPipe implements PipeTransform {
  transform(value: number | null | undefined): string {
    if (value == null) return '—';
    if (value > 0) return `+${value}`;
    if (value < 0) return `−${Math.abs(value)}`;
    return '±0';
  }
}

/** "VeryGood" → "Very good". */
@Pipe({ name: 'band', standalone: true })
export class BandPipe implements PipeTransform {
  transform(value: string | null | undefined): string {
    if (!value) return '';
    const spaced = value.replace(/([a-z])([A-Z])/g, '$1 $2');
    return spaced.charAt(0) + spaced.slice(1).toLowerCase();
  }
}

export function splitAmount(value: number): { whole: string; cents: string } {
  const [whole, cents] = amountFormat.format(value).split('.');
  return { whole, cents };
}
