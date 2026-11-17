using Wallet.Domain.Wallets;

namespace Wallet.Application.Features.Wallets;

internal static class LedgerMappings
{
    public static TransactionReceiptDto ToReceipt(this LedgerEntry entry, string currency) =>
        new(entry.Id, entry.Reference, entry.Type.ToString(), entry.Amount, entry.BalanceAfter, currency, entry.PostedAtUtc);
}
