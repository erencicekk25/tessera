using Tessera.SharedKernel.Domain;

namespace Scoring.Domain.CreditProfiles;

/// <summary>One point on the score timeline, with the reason the score moved.</summary>
public sealed class ScoreSnapshot : Entity<Guid>
{
    internal ScoreSnapshot(Guid profileId, int score, int delta, string reason, DateTime recordedAtUtc)
        : base(Guid.NewGuid())
    {
        ProfileId = profileId;
        Score = score;
        Delta = delta;
        Reason = reason;
        RecordedAtUtc = recordedAtUtc;
    }

    private ScoreSnapshot() { }

    public Guid ProfileId { get; private set; }

    public int Score { get; private set; }

    public int Delta { get; private set; }

    public string Reason { get; private set; } = string.Empty;

    public DateTime RecordedAtUtc { get; private set; }
}
