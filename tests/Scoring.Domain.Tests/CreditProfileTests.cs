using Scoring.Domain.CreditProfiles;
using Scoring.Domain.Policies;

namespace Scoring.Domain.Tests;

public sealed class CreditProfileTests
{
    private static readonly DateTime Now = new(2026, 11, 17, 9, 30, 0, DateTimeKind.Utc);

    private static TransactionSignal Signal(SignalKind kind, decimal amount, decimal balanceAfter, int minute = 0) =>
        new(kind, amount, balanceAfter, Now.AddMinutes(minute));

    [Fact]
    public void A_new_file_starts_at_the_baseline_in_the_fair_band()
    {
        var profile = CreditProfile.Open(Guid.NewGuid(), "Ada", Now);

        Assert.Equal(CreditScorePolicy.Baseline, profile.Score);
        Assert.Equal(ScoreBand.Fair, profile.Band);
        Assert.Single(profile.History);
    }

    [Fact]
    public void Healthy_inflows_raise_the_score()
    {
        var profile = CreditProfile.Open(Guid.NewGuid(), "Ada", Now);

        var delta = profile.Apply(Signal(SignalKind.Deposit, 10_000m, 10_000m));

        Assert.True(delta > 0);
        Assert.True(profile.Score > CreditScorePolicy.Baseline);
    }

    [Fact]
    public void A_declined_payment_lowers_the_score()
    {
        var profile = CreditProfile.Open(Guid.NewGuid(), "Ada", Now);
        profile.Apply(Signal(SignalKind.Deposit, 1_000m, 1_000m));
        var before = profile.Score;

        var delta = profile.Apply(Signal(SignalKind.Declined, 5_000m, 1_000m, 1));

        Assert.True(delta < 0);
        Assert.Equal(before + delta, profile.Score);
        Assert.Equal(1, profile.DeclinedCount);
    }

    [Fact]
    public void Draining_most_of_the_balance_counts_as_a_volatility_event()
    {
        var profile = CreditProfile.Open(Guid.NewGuid(), "Ada", Now);
        profile.Apply(Signal(SignalKind.Deposit, 1_000m, 1_000m));

        profile.Apply(Signal(SignalKind.Withdrawal, 700m, 300m, 1));
        profile.Apply(Signal(SignalKind.Withdrawal, 50m, 250m, 2));

        Assert.Equal(1, profile.DrainEventCount);
    }

    [Fact]
    public void Score_never_leaves_the_300_to_850_range()
    {
        var profile = CreditProfile.Open(Guid.NewGuid(), "Ada", Now);

        for (var i = 0; i < 20; i++)
        {
            profile.Apply(Signal(SignalKind.Declined, 100m, 0m, i));
        }

        Assert.InRange(profile.Score, ScoreScale.Min, ScoreScale.Max);
        Assert.Equal(ScoreBand.Poor, profile.Band);
    }

    [Fact]
    public void History_is_capped()
    {
        var profile = CreditProfile.Open(Guid.NewGuid(), "Ada", Now);

        for (var i = 1; i <= CreditProfile.HistoryDepth + 15; i++)
        {
            profile.Apply(Signal(SignalKind.Deposit, 10m, 10m * i, i));
        }

        Assert.Equal(CreditProfile.HistoryDepth, profile.History.Count);
        Assert.DoesNotContain(profile.History, s => s.Reason == "Credit file opened");
    }

    [Fact]
    public void Factor_points_add_up_to_the_score()
    {
        var profile = CreditProfile.Open(Guid.NewGuid(), "Ada", Now);
        profile.Apply(Signal(SignalKind.Deposit, 4_000m, 4_000m));
        profile.Apply(Signal(SignalKind.TransferOut, 500m, 3_500m, 1));

        var breakdown = CreditScorePolicy.Evaluate(profile);

        Assert.Equal(profile.Score, CreditScorePolicy.Baseline + breakdown.Factors.Sum(f => f.Points));
    }
}
