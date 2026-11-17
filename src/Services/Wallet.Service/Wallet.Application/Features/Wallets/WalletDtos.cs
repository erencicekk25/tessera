namespace Wallet.Application.Features.Wallets;

public sealed record WalletSummaryDto(
    Guid WalletId,
    string OwnerName,
    string OwnerEmail,
    string Currency,
    decimal Balance,
    decimal TotalInflow,
    decimal TotalOutflow,
    int EntryCount,
    DateTime OpenedAtUtc,
    DateTime? LastActivityAtUtc);

public sealed record LedgerEntryDto(
    Guid Id,
    string Type,
    string Direction,
    decimal Amount,
    decimal BalanceAfter,
    string Description,
    string Reference,
    string? Counterparty,
    DateTime PostedAtUtc);

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount)
{
    public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}

public sealed record TransactionReceiptDto(
    Guid EntryId,
    string Reference,
    string Type,
    decimal Amount,
    decimal BalanceAfter,
    string Currency,
    DateTime PostedAtUtc);

public static class WalletCacheKeys
{
    public static string Summary(Guid ownerId) => $"wallet:summary:{ownerId:N}";
}
