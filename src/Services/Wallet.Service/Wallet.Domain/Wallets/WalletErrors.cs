using Tessera.SharedKernel.Results;

namespace Wallet.Domain.Wallets;

public static class WalletErrors
{
    public static readonly Error NotFound = Error.NotFound("Wallet.NotFound", "No wallet exists for this account.");
    public static readonly Error RecipientNotFound = Error.NotFound("Wallet.RecipientNotFound", "No wallet is registered for the recipient e-mail.");
    public static readonly Error AmountMustBePositive = Error.Validation("Wallet.AmountMustBePositive", "Amount must be greater than zero.");
    public static readonly Error TooManyDecimals = Error.Validation("Wallet.TooManyDecimals", "Amount cannot have more than two decimal places.");
    public static readonly Error AmountAboveLimit = Error.BusinessRule("Wallet.AmountAboveLimit", $"A single transaction cannot exceed {Money.MaxSingleTransaction:N0}.");
    public static readonly Error CurrencyMismatch = Error.BusinessRule("Wallet.CurrencyMismatch", "The amount currency does not match the wallet currency.");
    public static readonly Error InsufficientFunds = Error.BusinessRule("Wallet.InsufficientFunds", "The wallet balance is too low for this transaction.");
    public static readonly Error SelfTransfer = Error.BusinessRule("Wallet.SelfTransfer", "You cannot send money to your own wallet.");
}
