using Scoring.Domain.CreditProfiles;
using Scoring.Domain.Policies;

namespace Scoring.Application.Features.CreditReports;

public sealed record CreditReportDto(
    Guid UserId,
    string DisplayName,
    int Score,
    string Band,
    int MinScore,
    int MaxScore,
    int LastDelta,
    string? LastReason,
    IReadOnlyList<ScoreFactorDto> Factors,
    IReadOnlyList<ScoreSnapshotDto> History,
    CreditStatsDto Stats,
    DateTime UpdatedAtUtc);

public sealed record ScoreFactorDto(string Key, string Label, int Points, int MinPoints, int MaxPoints, string Impact, string Explanation);

public sealed record ScoreSnapshotDto(int Score, int Delta, string Reason, DateTime RecordedAtUtc);

public sealed record CreditStatsDto(
    decimal TotalInflow,
    decimal TotalOutflow,
    decimal LastKnownBalance,
    decimal PeakBalance,
    int TransactionCount,
    int DeclinedCount,
    int DrainEventCount);

internal static class CreditReportMapper
{
    public static CreditReportDto ToReport(this CreditProfile profile)
    {
        var breakdown = CreditScorePolicy.Evaluate(profile);
        var history = profile.History.OrderBy(h => h.RecordedAtUtc).ToList();
        var last = history.LastOrDefault();

        return new CreditReportDto(
            profile.Id,
            profile.DisplayName,
            profile.Score,
            profile.Band.ToString(),
            ScoreScale.Min,
            ScoreScale.Max,
            last?.Delta ?? 0,
            last?.Reason,
            breakdown.Factors
                .Select(f => new ScoreFactorDto(f.Key, f.Label, f.Points, f.MinPoints, f.MaxPoints, f.Impact, f.Explanation))
                .ToList(),
            history.Select(h => new ScoreSnapshotDto(h.Score, h.Delta, h.Reason, h.RecordedAtUtc)).ToList(),
            new CreditStatsDto(
                profile.TotalInflow,
                profile.TotalOutflow,
                profile.LastKnownBalance,
                profile.PeakBalance,
                profile.TransactionCount,
                profile.DeclinedCount,
                profile.DrainEventCount),
            profile.UpdatedAtUtc);
    }
}
