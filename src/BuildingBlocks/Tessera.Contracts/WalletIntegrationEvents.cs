namespace Tessera.Contracts;

/// <summary>Published by Wallet.Service when a new customer signs up and receives a wallet.</summary>
public sealed record UserRegisteredIntegrationEvent(
    Guid EventId,
    Guid UserId,
    string FullName,
    DateTime OccurredAtUtc);

/// <summary>Published by Wallet.Service for every ledger entry that is posted successfully.</summary>
public sealed record TransactionCompletedIntegrationEvent(
    Guid EventId,
    Guid UserId,
    Guid WalletId,
    Guid LedgerEntryId,
    TransactionKind Kind,
    decimal Amount,
    decimal BalanceAfter,
    string Currency,
    DateTime OccurredAtUtc);

/// <summary>Published by Wallet.Service when an outgoing transaction is rejected (e.g. insufficient funds).</summary>
public sealed record TransactionDeclinedIntegrationEvent(
    Guid EventId,
    Guid UserId,
    Guid WalletId,
    TransactionKind Kind,
    decimal Amount,
    decimal CurrentBalance,
    string Reason,
    DateTime OccurredAtUtc);

public enum TransactionKind
{
    Deposit = 1,
    Withdrawal = 2,
    TransferIn = 3,
    TransferOut = 4
}
