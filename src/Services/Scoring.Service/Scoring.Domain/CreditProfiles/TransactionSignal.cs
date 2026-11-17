namespace Scoring.Domain.CreditProfiles;

public enum SignalKind
{
    Deposit = 1,
    Withdrawal = 2,
    TransferIn = 3,
    TransferOut = 4,
    Declined = 5
}

/// <summary>
/// A behavioural observation about a customer's money, translated from the Wallet context's
/// integration events (anti-corruption: Scoring never depends on Wallet's internal model).
/// </summary>
public sealed record TransactionSignal(SignalKind Kind, decimal Amount, decimal BalanceAfter, DateTime OccurredAtUtc)
{
    public bool IsInflow => Kind is SignalKind.Deposit or SignalKind.TransferIn;

    public bool IsOutflow => Kind is SignalKind.Withdrawal or SignalKind.TransferOut;

    public string Describe() => Kind switch
    {
        SignalKind.Deposit => $"Deposit of {Amount:N2}",
        SignalKind.Withdrawal => $"Withdrawal of {Amount:N2}",
        SignalKind.TransferIn => $"Incoming transfer of {Amount:N2}",
        SignalKind.TransferOut => $"Outgoing transfer of {Amount:N2}",
        _ => $"Declined payment of {Amount:N2}"
    };
}
