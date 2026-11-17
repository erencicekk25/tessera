import { ScoreBand, ScoreFactor } from '../models';

/**
 * TypeScript mirror of Scoring.Domain.Policies.CreditScorePolicy — used only by demo mode.
 * Keep the two in sync: the C# policy is the source of truth.
 */
export interface ScoringInputs {
  totalInflow: number;
  totalOutflow: number;
  lastKnownBalance: number;
  transactionCount: number;
  declinedCount: number;
  drainEventCount: number;
}

export const BASELINE = 600;
export const MIN_SCORE = 300;
export const MAX_SCORE = 850;
export const DRAIN_THRESHOLD = 0.6;

export function bandFor(score: number): ScoreBand {
  if (score >= 800) return 'Exceptional';
  if (score >= 740) return 'VeryGood';
  if (score >= 670) return 'Good';
  if (score >= 580) return 'Fair';
  return 'Poor';
}

function factor(key: string, label: string, points: number, minPoints: number, maxPoints: number, explanation: string): ScoreFactor {
  return { key, label, points, minPoints, maxPoints, explanation, impact: points > 0 ? 'positive' : points < 0 ? 'negative' : 'neutral' };
}

export function evaluate(p: ScoringInputs): { score: number; band: ScoreBand; factors: ScoreFactor[] } {
  const discipline =
    p.declinedCount === 0
      ? factor('payment-discipline', 'Payment discipline', p.transactionCount >= 5 ? 30 : 0, -175, 30,
          p.transactionCount >= 5 ? 'No declined payments across your recent activity.' : 'Complete at least 5 transactions without a decline to earn this bonus.')
      : factor('payment-discipline', 'Payment discipline', -Math.min(p.declinedCount * 35, 175), -175, 30,
          `${p.declinedCount} payment(s) bounced for insufficient funds.`);

  const depth = factor('balance-depth', 'Balance depth',
    Math.round(Math.min(Math.max(p.lastKnownBalance / 25_000, 0), 1) * 100), 0, 100, 'A cushion of 25,000 or more earns full marks.');

  let cashFlow: ScoreFactor;
  if (p.totalInflow === 0) {
    cashFlow = factor('cash-flow', 'Cash-flow ratio', 0, -60, 60, 'No inflows recorded yet.');
  } else {
    const ratio = Math.min(Math.max((p.totalInflow - p.totalOutflow) / p.totalInflow, -1), 1);
    cashFlow = factor('cash-flow', 'Cash-flow ratio', Math.round(ratio * 60), -60, 60, `You keep ${Math.round(Math.max(ratio, 0) * 100)}% of what comes in.`);
  }

  const activity = factor('activity', 'Account activity', Math.round((Math.min(p.transactionCount, 40) / 40) * 50), 0, 50,
    `${p.transactionCount} settled transaction(s); 40 or more earns full marks.`);

  const volatility = factor('volatility', 'Balance volatility', -Math.min(p.drainEventCount * 15, 90), -90, 0,
    p.drainEventCount === 0 ? 'No sudden drains of your balance.' : `${p.drainEventCount} outflow(s) took more than 60% of your balance at once.`);

  const factors = [discipline, depth, cashFlow, activity, volatility];
  const score = Math.min(Math.max(BASELINE + factors.reduce((sum, f) => sum + f.points, 0), MIN_SCORE), MAX_SCORE);
  return { score, band: bandFor(score), factors };
}
