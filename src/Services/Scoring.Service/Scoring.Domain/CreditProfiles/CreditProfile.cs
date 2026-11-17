using Scoring.Domain.Policies;
using Tessera.SharedKernel.Domain;

namespace Scoring.Domain.CreditProfiles;

/// <summary>
/// A customer's behavioural credit file. Keyed by the user id issued by the identity provider.
/// It keeps running aggregates (not raw transactions) — enough to explain the score, nothing more.
/// </summary>
public sealed class CreditProfile : AggregateRoot<Guid>
{
    public const int HistoryDepth = 60;

    /// <summary>An outflow larger than this share of the pre-transaction balance counts as a "drain".</summary>
    public const decimal DrainThreshold = 0.6m;

    private readonly List<ScoreSnapshot> _history = [];

    private CreditProfile(Guid userId, string displayName, DateTime openedAtUtc) : base(userId)
    {
        DisplayName = displayName;
        OpenedAtUtc = openedAtUtc;
        UpdatedAtUtc = openedAtUtc;
    }

    private CreditProfile() { }

    public string DisplayName { get; private set; } = string.Empty;

    public int Score { get; private set; }

    public ScoreBand Band { get; private set; }

    public decimal TotalInflow { get; private set; }

    public decimal TotalOutflow { get; private set; }

    public decimal LastKnownBalance { get; private set; }

    public decimal PeakBalance { get; private set; }

    public int TransactionCount { get; private set; }

    public int DeclinedCount { get; private set; }

    public int DrainEventCount { get; private set; }

    public DateTime OpenedAtUtc { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    public uint Version { get; private set; }

    public IReadOnlyCollection<ScoreSnapshot> History => _history.AsReadOnly();

    public static CreditProfile Open(Guid userId, string? displayName, DateTime nowUtc)
    {
        var profile = new CreditProfile(userId, string.IsNullOrWhiteSpace(displayName) ? "Customer" : displayName.Trim(), nowUtc);
        var breakdown = CreditScorePolicy.Evaluate(profile);

        profile.Score = breakdown.Score;
        profile.Band = breakdown.Band;
        profile._history.Add(new ScoreSnapshot(userId, breakdown.Score, 0, "Credit file opened", nowUtc));
        return profile;
    }

    public void Rename(string displayName)
    {
        if (!string.IsNullOrWhiteSpace(displayName))
        {
            DisplayName = displayName.Trim();
        }
    }

    /// <summary>Folds a new signal into the file and re-scores it. Returns the score movement.</summary>
    public int Apply(TransactionSignal signal)
    {
        if (signal.IsInflow)
        {
            TotalInflow += signal.Amount;
            TransactionCount++;
        }
        else if (signal.IsOutflow)
        {
            var balanceBefore = signal.BalanceAfter + signal.Amount;
            if (balanceBefore > 0 && signal.Amount > balanceBefore * DrainThreshold)
            {
                DrainEventCount++;
            }

            TotalOutflow += signal.Amount;
            TransactionCount++;
        }
        else
        {
            DeclinedCount++;
        }

        LastKnownBalance = signal.BalanceAfter;
        PeakBalance = Math.Max(PeakBalance, signal.BalanceAfter);
        UpdatedAtUtc = signal.OccurredAtUtc > UpdatedAtUtc ? signal.OccurredAtUtc : UpdatedAtUtc;

        var breakdown = CreditScorePolicy.Evaluate(this);
        var delta = breakdown.Score - Score;

        Score = breakdown.Score;
        Band = breakdown.Band;

        _history.Add(new ScoreSnapshot(Id, Score, delta, signal.Describe(), signal.OccurredAtUtc));
        TrimHistory();

        return delta;
    }

    private void TrimHistory()
    {
        while (_history.Count > HistoryDepth)
        {
            var oldest = _history.MinBy(s => s.RecordedAtUtc)!;
            _history.Remove(oldest);
        }
    }
}
