using Scoring.Domain.CreditProfiles;

namespace Scoring.Domain.Policies;

public sealed record ScoreFactor(string Key, string Label, int Points, int MinPoints, int MaxPoints, string Explanation)
{
    public string Impact => Points switch
    {
        > 0 => "positive",
        < 0 => "negative",
        _ => "neutral"
    };
}

public sealed record ScoreBreakdown(int Score, ScoreBand Band, IReadOnlyList<ScoreFactor> Factors);

/// <summary>
/// Deterministic, explainable scoring model (a simulation — not a real bureau algorithm).
/// <code>
/// score = 600 (baseline)
///       + payment discipline   [-175 … +30]
///       + balance depth        [   0 … +100]
///       + cash-flow ratio      [ -60 … +60]
///       + activity             [   0 … +50]
///       + volatility           [ -90 … 0]
/// clamped to 300 … 850
/// </code>
/// Every factor is returned with its contribution, so the UI can show exactly why a score moved.
/// </summary>
public static class CreditScorePolicy
{
    public const int Baseline = 600;

    private const decimal BalanceDepthCeiling = 25_000m;
    private const int ActivityCeiling = 40;

    public static ScoreBreakdown Evaluate(CreditProfile profile)
    {
        var factors = new List<ScoreFactor>
        {
            PaymentDiscipline(profile),
            BalanceDepth(profile),
            CashFlow(profile),
            Activity(profile),
            Volatility(profile)
        };

        var score = ScoreScale.Clamp(Baseline + factors.Sum(f => f.Points));
        return new ScoreBreakdown(score, ScoreScale.BandFor(score), factors);
    }

    private static ScoreFactor PaymentDiscipline(CreditProfile p)
    {
        int points;
        string explanation;

        if (p.DeclinedCount == 0)
        {
            points = p.TransactionCount >= 5 ? 30 : 0;
            explanation = points > 0
                ? "No declined payments across your recent activity."
                : "Complete at least 5 transactions without a decline to earn this bonus.";
        }
        else
        {
            points = -Math.Min(p.DeclinedCount * 35, 175);
            explanation = $"{p.DeclinedCount} payment(s) bounced for insufficient funds.";
        }

        return new ScoreFactor("payment-discipline", "Payment discipline", points, -175, 30, explanation);
    }

    private static ScoreFactor BalanceDepth(CreditProfile p)
    {
        var ratio = Math.Clamp(p.LastKnownBalance / BalanceDepthCeiling, 0m, 1m);
        var points = (int)Math.Round(ratio * 100m, MidpointRounding.AwayFromZero);
        return new ScoreFactor("balance-depth", "Balance depth", points, 0, 100,
            $"A cushion of {BalanceDepthCeiling:N0} or more earns full marks.");
    }

    private static ScoreFactor CashFlow(CreditProfile p)
    {
        if (p.TotalInflow == 0)
        {
            return new ScoreFactor("cash-flow", "Cash-flow ratio", 0, -60, 60, "No inflows recorded yet.");
        }

        var ratio = Math.Clamp((p.TotalInflow - p.TotalOutflow) / p.TotalInflow, -1m, 1m);
        var points = (int)Math.Round(ratio * 60m, MidpointRounding.AwayFromZero);
        return new ScoreFactor("cash-flow", "Cash-flow ratio", points, -60, 60,
            $"You keep {Math.Max(ratio, 0m):P0} of what comes in.");
    }

    private static ScoreFactor Activity(CreditProfile p)
    {
        var points = (int)Math.Round(Math.Min(p.TransactionCount, ActivityCeiling) / (double)ActivityCeiling * 50, MidpointRounding.AwayFromZero);
        return new ScoreFactor("activity", "Account activity", points, 0, 50,
            $"{p.TransactionCount} settled transaction(s); {ActivityCeiling} or more earns full marks.");
    }

    private static ScoreFactor Volatility(CreditProfile p)
    {
        var points = -Math.Min(p.DrainEventCount * 15, 90);
        return new ScoreFactor("volatility", "Balance volatility", points, -90, 0,
            p.DrainEventCount == 0
                ? "No sudden drains of your balance."
                : $"{p.DrainEventCount} outflow(s) took more than {CreditProfile.DrainThreshold:P0} of your balance at once.");
    }
}
