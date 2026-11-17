using Tessera.SharedKernel.Results;

namespace Wallet.Domain.Wallets;

/// <summary>
/// A strictly positive transaction amount in a specific currency, limited to minor units.
/// Balances may be zero; transaction amounts never are.
/// </summary>
public sealed record Money
{
    public const decimal MaxSingleTransaction = 250_000m;

    private Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public decimal Amount { get; }

    public string Currency { get; }

    public static Result<Money> Create(decimal amount, string currency)
    {
        if (amount <= 0)
        {
            return WalletErrors.AmountMustBePositive;
        }

        if (decimal.Round(amount, 2) != amount)
        {
            return WalletErrors.TooManyDecimals;
        }

        if (amount > MaxSingleTransaction)
        {
            return WalletErrors.AmountAboveLimit;
        }

        return new Money(amount, currency.ToUpperInvariant());
    }

    public override string ToString() => $"{Amount:0.00} {Currency}";
}
