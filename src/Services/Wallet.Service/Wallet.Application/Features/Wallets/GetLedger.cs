using FluentValidation;
using Tessera.SharedKernel.Results;
using Tessera.Application.Messaging;
using Wallet.Application.Abstractions;

namespace Wallet.Application.Features.Wallets;

public sealed record GetLedgerQuery(Guid UserId, int Page = 1, int PageSize = 10) : IQuery<PagedResult<LedgerEntryDto>>;

internal sealed class GetLedgerQueryValidator : AbstractValidator<GetLedgerQuery>
{
    public GetLedgerQueryValidator()
    {
        RuleFor(q => q.Page).GreaterThanOrEqualTo(1);
        RuleFor(q => q.PageSize).InclusiveBetween(1, 100);
    }
}

internal sealed class GetLedgerQueryHandler(IWalletReadModel readModel)
    : IQueryHandler<GetLedgerQuery, PagedResult<LedgerEntryDto>>
{
    public async Task<Result<PagedResult<LedgerEntryDto>>> Handle(GetLedgerQuery query, CancellationToken cancellationToken) =>
        await readModel.GetLedgerAsync(query.UserId, query.Page, query.PageSize, cancellationToken);
}
