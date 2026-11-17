namespace Scoring.Domain.CreditProfiles;

/// <summary>Bands loosely modelled on common 300–850 consumer scoring ranges.</summary>
public enum ScoreBand
{
    Poor = 1,
    Fair = 2,
    Good = 3,
    VeryGood = 4,
    Exceptional = 5
}

public static class ScoreScale
{
    public const int Min = 300;
    public const int Max = 850;

    public static ScoreBand BandFor(int score) => score switch
    {
        >= 800 => ScoreBand.Exceptional,
        >= 740 => ScoreBand.VeryGood,
        >= 670 => ScoreBand.Good,
        >= 580 => ScoreBand.Fair,
        _ => ScoreBand.Poor
    };

    public static int Clamp(int score) => Math.Clamp(score, Min, Max);
}
