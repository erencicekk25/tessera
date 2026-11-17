using Microsoft.EntityFrameworkCore;
using Wallet.Application.Abstractions;
using Wallet.Application.Features.Wallets;
using Wallet.Domain.Wallets;

namespace Wallet.Infrastructure.Persistence;

/// <summary>Read-side projections. No tracking, no aggregates — just the shape the client needs.</summary>
internal sealed class WalletReadModel(WalletDbContext db) : IWalletReadModel
{
    public async Task<WalletSummaryDto?> GetSummaryAsync(Guid ownerId, CancellationToken cancellationToken)
    {
        var header = await (
                from w in db.Wallets.AsNoTracking()
                join u in db.Users.AsNoTracking() on w.OwnerId equals u.Id
                where w.OwnerId == ownerId
                select new { w.Id, u.FullName, Email = u.Email, w.Currency, w.Balance, w.OpenedAtUtc })
            .FirstOrDefaultAsync(cancellationToken);

        if (header is null)
        {
            return null;
        }

        var totals = await db.LedgerEntries.AsNoTracking()
            .Where(e => e.WalletId == header.Id)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Inflow = g.Sum(e => e.Direction == EntryDirection.Credit ? e.Amount : 0m),
                Outflow = g.Sum(e => e.Direction == EntryDirection.Debit ? e.Amount : 0m),
                Count = g.Count(),
                Last = g.Max(e => (DateTime?)e.PostedAtUtc)
            })
            .FirstOrDefaultAsync(cancellationToken);

        return new WalletSummaryDto(
            header.Id,
            header.FullName,
            header.Email.Value,
            header.Currency,
            header.Balance,
            totals?.Inflow ?? 0m,
            totals?.Outflow ?? 0m,
            totals?.Count ?? 0,
            header.OpenedAtUtc,
            totals?.Last);
    }

    public async Task<PagedResult<LedgerEntryDto>> GetLedgerAsync(Guid ownerId, int page, int pageSize, CancellationToken cancellationToken)
    {
        var entries = db.LedgerEntries.AsNoTracking()
            .Where(e => db.Wallets.Any(w => w.Id == e.WalletId && w.OwnerId == ownerId));

        var total = await entries.CountAsync(cancellationToken);

        var items = await entries
            .OrderByDescending(e => e.PostedAtUtc)
            .ThenByDescending(e => e.Reference)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(e => new LedgerEntryDto(
                e.Id,
                e.Type.ToString(),
                e.Direction.ToString(),
                e.Amount,
                e.BalanceAfter,
                e.Description,
                e.Reference,
                e.Counterparty,
                e.PostedAtUtc))
            .ToListAsync(cancellationToken);

        return new PagedResult<LedgerEntryDto>(items, page, pageSize, total);
    }
}
