using FluentValidation;
using Tessera.SharedKernel.Results;
using Tessera.Application.Messaging;
using Wallet.Application.Abstractions;
using Wallet.Domain.Abstractions;
using Wallet.Domain.Wallets;

namespace Wallet.Application.Features.Wallets;

public sealed record DepositFundsCommand(Guid UserId, decimal Amount, string? Description) : ICommand<TransactionReceiptDto>;

internal sealed class DepositFundsCommandValidator : AbstractValidator<DepositFundsCommand>
{
    public DepositFundsCommandValidator()
    {
        RuleFor(c => c.Amount).GreaterThan(0).LessThanOrEqualTo(Money.MaxSingleTransaction).PrecisionScale(12, 2, true);
        RuleFor(c => c.Description).MaximumLength(140);
    }
}

internal sealed class DepositFundsCommandHandler(
    IWalletRepository wallets,
    IUnitOfWork unitOfWork,
    ICacheService cache,
    TimeProvider clock) : ICommandHandler<DepositFundsCommand, TransactionReceiptDto>
{
    public async Task<Result<TransactionReceiptDto>> Handle(DepositFundsCommand command, CancellationToken cancellationToken)
    {
        var wallet = await wallets.GetByOwnerAsync(command.UserId, cancellationToken);
        if (wallet is null)
        {
            return WalletErrors.NotFound;
        }

        var money = Money.Create(command.Amount, wallet.Currency);
        if (money.IsFailure)
        {
            return money.Error;
        }

        var entry = wallet.Deposit(money.Value, command.Description, clock.GetUtcNow().UtcDateTime);
        if (entry.IsFailure)
        {
            return entry.Error;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        await cache.RemoveAsync(WalletCacheKeys.Summary(command.UserId), cancellationToken);

        return entry.Value.ToReceipt(wallet.Currency);
    }
}
