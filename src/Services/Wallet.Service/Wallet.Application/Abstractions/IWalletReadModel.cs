using Wallet.Application.Features.Wallets;

namespace Wallet.Application.Abstractions;

/// <summary>
/// Query side of CQRS: projections straight from storage into DTOs, bypassing aggregates.
/// </summary>
public interface IWalletReadModel
{
    Task<WalletSummaryDto?> GetSummaryAsync(Guid ownerId, CancellationToken cancellationToken);

    Task<PagedResult<LedgerEntryDto>> GetLedgerAsync(Guid ownerId, int page, int pageSize, CancellationToken cancellationToken);
}
