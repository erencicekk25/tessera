using Tessera.SharedKernel.Domain;

namespace Wallet.Domain.Wallets;

public enum LedgerEntryType
{
    Deposit = 1,
    Withdrawal = 2,
    TransferIn = 3,
    TransferOut = 4
}

public enum EntryDirection
{
    Credit = 1,
    Debit = 2
}

/// <summary>
/// Immutable line in the wallet's general ledger. Entries are append-only: corrections are
/// new entries, never edits. <see cref="BalanceAfter"/> makes the running balance auditable.
/// </summary>
public sealed class LedgerEntry : Entity<Guid>
{
    internal LedgerEntry(
        Guid walletId,
        LedgerEntryType type,
        decimal amount,
        decimal balanceAfter,
        string description,
        string reference,
        string? counterparty,
        Guid? correlationId,
        DateTime postedAtUtc)
        : base(Guid.NewGuid())
    {
        WalletId = walletId;
        Type = type;
        Direction = type is LedgerEntryType.Deposit or LedgerEntryType.TransferIn ? EntryDirection.Credit : EntryDirection.Debit;
        Amount = amount;
        BalanceAfter = balanceAfter;
        Description = description;
        Reference = reference;
        Counterparty = counterparty;
        CorrelationId = correlationId;
        PostedAtUtc = postedAtUtc;
    }

    private LedgerEntry() { }

    public Guid WalletId { get; private set; }

    public LedgerEntryType Type { get; private set; }

    public EntryDirection Direction { get; private set; }

    public decimal Amount { get; private set; }

    public decimal BalanceAfter { get; private set; }

    public string Description { get; private set; } = string.Empty;

    /// <summary>Human-friendly voucher number, e.g. <c>DEP-7Q2K9XH4</c>.</summary>
    public string Reference { get; private set; } = string.Empty;

    public string? Counterparty { get; private set; }

    /// <summary>Links both legs of a transfer.</summary>
    public Guid? CorrelationId { get; private set; }

    public DateTime PostedAtUtc { get; private set; }
}
