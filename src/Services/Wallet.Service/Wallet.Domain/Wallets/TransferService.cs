using Tessera.SharedKernel.Results;

namespace Wallet.Domain.Wallets;

/// <summary>
/// Domain service coordinating a peer-to-peer transfer. The operation spans two aggregates,
/// so it lives outside both; the application layer commits them in a single database transaction.
/// </summary>
public static class TransferService
{
    public sealed record TransferLegs(Guid TransferId, LedgerEntry Outgoing, LedgerEntry Incoming);

    public static Result<TransferLegs> Transfer(
        DigitalWallet source,
        DigitalWallet destination,
        Money amount,
        string sourceLabel,
        string destinationLabel,
        string? note,
        DateTime nowUtc)
    {
        if (source.Id == destination.Id || source.OwnerId == destination.OwnerId)
        {
            return WalletErrors.SelfTransfer;
        }

        if (destination.Currency != amount.Currency)
        {
            return WalletErrors.CurrencyMismatch;
        }

        var canDebit = source.EnsureCanDebit(amount);
        if (canDebit.IsFailure)
        {
            return canDebit.Error;
        }

        var transferId = Guid.NewGuid();
        var trimmedNote = string.IsNullOrWhiteSpace(note) ? null : note.Trim();

        var outgoing = source.PostTransferOut(amount, destinationLabel, trimmedNote, transferId, nowUtc);
        var incoming = destination.PostTransferIn(amount, sourceLabel, trimmedNote, transferId, nowUtc);

        return new TransferLegs(transferId, outgoing, incoming);
    }
}
