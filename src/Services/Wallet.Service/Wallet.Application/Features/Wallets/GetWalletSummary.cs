using Tessera.SharedKernel.Results;
using Tessera.Application.Messaging;
using Wallet.Application.Abstractions;
using Wallet.Domain.Wallets;

namespace Wallet.Application.Features.Wallets;

/// <summary>Balance + headline figures. Served from Redis; evicted by every write to the wallet.</summary>
public sealed record GetWalletSummaryQuery(Guid UserId) : IQuery<WalletSummaryDto>, ICachedQuery
{
    public string CacheKey => WalletCacheKeys.Summary(UserId);

    public TimeSpan? Expiration => TimeSpan.FromMinutes(5);
}

internal sealed class GetWalletSummaryQueryHandler(IWalletReadModel readModel)
    : IQueryHandler<GetWalletSummaryQuery, WalletSummaryDto>
{
    public async Task<Result<WalletSummaryDto>> Handle(GetWalletSummaryQuery query, CancellationToken cancellationToken)
    {
        var summary = await readModel.GetSummaryAsync(query.UserId, cancellationToken);
        return summary is null ? WalletErrors.NotFound : summary;
    }
}
