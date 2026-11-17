using System.Security.Cryptography;
using Tessera.SharedKernel.Domain;
using Tessera.SharedKernel.Results;

namespace Wallet.Domain.Wallets;

/// <summary>
/// Consistency boundary for a customer's money. Every balance change goes through this aggregate
/// and produces exactly one <see cref="LedgerEntry"/>, so <c>Balance == sum(credits) - sum(debits)</c> always holds.
/// </summary>
public sealed class DigitalWallet : AggregateRoot<Guid>
{
    public const string DefaultCurrency = "TRY";

    private readonly List<LedgerEntry> _entries = [];

    private DigitalWallet(Guid id, Guid ownerId, string currency, DateTime openedAtUtc) : base(id)
    {
        OwnerId = ownerId;
        Currency = currency;
        OpenedAtUtc = openedAtUtc;
    }

    private DigitalWallet() { }

    public Guid OwnerId { get; private set; }

    public string Currency { get; private set; } = DefaultCurrency;

    public decimal Balance { get; private set; }

    public DateTime OpenedAtUtc { get; private set; }

    /// <summary>Optimistic concurrency token (mapped to PostgreSQL <c>xmin</c>).</summary>
    public uint Version { get; private set; }

    /// <summary>
    /// Entries posted during the current unit of work. Historic entries are intentionally not
    /// loaded with the aggregate; the ledger is read through a dedicated query model.
    /// </summary>
    public IReadOnlyCollection<LedgerEntry> Entries => _entries.AsReadOnly();

    public static DigitalWallet Open(Guid ownerId, DateTime nowUtc, string currency = DefaultCurrency) =>
        new(Guid.NewGuid(), ownerId, currency, nowUtc);

    public Result<LedgerEntry> Deposit(Money amount, string? description, DateTime nowUtc)
    {
        if (amount.Currency != Currency)
        {
            return WalletErrors.CurrencyMismatch;
        }

        return Post(LedgerEntryType.Deposit, amount.Amount, Describe(description, "Cash deposit"), null, null, nowUtc);
    }

    public Result<LedgerEntry> Withdraw(Money amount, string? description, DateTime nowUtc)
    {
        var check = EnsureCanDebit(amount);
        return check.IsFailure
            ? Result.Failure<LedgerEntry>(check.Error)
            : Post(LedgerEntryType.Withdrawal, amount.Amount, Describe(description, "Cash withdrawal"), null, null, nowUtc);
    }

    internal Result EnsureCanDebit(Money amount)
    {
        if (amount.Currency != Currency)
        {
            return WalletErrors.CurrencyMismatch;
        }

        return Balance < amount.Amount ? WalletErrors.InsufficientFunds : Result.Success();
    }

    internal LedgerEntry PostTransferOut(Money amount, string counterparty, string? note, Guid transferId, DateTime nowUtc) =>
        Post(LedgerEntryType.TransferOut, amount.Amount, note ?? $"Transfer to {counterparty}", counterparty, transferId, nowUtc).Value;

    internal LedgerEntry PostTransferIn(Money amount, string counterparty, string? note, Guid transferId, DateTime nowUtc) =>
        Post(LedgerEntryType.TransferIn, amount.Amount, note ?? $"Transfer from {counterparty}", counterparty, transferId, nowUtc).Value;

    private Result<LedgerEntry> Post(
        LedgerEntryType type,
        decimal amount,
        string description,
        string? counterparty,
        Guid? correlationId,
        DateTime nowUtc)
    {
        var isCredit = type is LedgerEntryType.Deposit or LedgerEntryType.TransferIn;
        var newBalance = isCredit ? Balance + amount : Balance - amount;

        if (newBalance < 0)
        {
            return WalletErrors.InsufficientFunds;
        }

        Balance = newBalance;

        var entry = new LedgerEntry(Id, type, amount, newBalance, description, NewReference(type), counterparty, correlationId, nowUtc);
        _entries.Add(entry);

        Raise(new LedgerEntryPostedDomainEvent(Id, OwnerId, entry.Id, type, amount, newBalance, Currency, nowUtc));
        return entry;
    }

    private static string Describe(string? description, string fallback) =>
        string.IsNullOrWhiteSpace(description) ? fallback : description.Trim();

    private static string NewReference(LedgerEntryType type)
    {
        const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var prefix = type switch
        {
            LedgerEntryType.Deposit => "DEP",
            LedgerEntryType.Withdrawal => "WDL",
            LedgerEntryType.TransferIn => "TRI",
            _ => "TRO"
        };

        return $"{prefix}-{RandomNumberGenerator.GetString(alphabet, 8)}";
    }
}

public sealed record LedgerEntryPostedDomainEvent(
    Guid WalletId,
    Guid OwnerId,
    Guid LedgerEntryId,
    LedgerEntryType Type,
    decimal Amount,
    decimal BalanceAfter,
    string Currency,
    DateTime OccurredAtUtc) : DomainEvent(OccurredAtUtc);
