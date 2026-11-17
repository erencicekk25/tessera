using FluentValidation;
using Tessera.Contracts;
using Tessera.SharedKernel.Results;
using Tessera.Application.Messaging;
using Wallet.Application.Abstractions;
using Wallet.Domain.Abstractions;
using Wallet.Domain.Wallets;

namespace Wallet.Application.Features.Wallets;

public sealed record WithdrawFundsCommand(Guid UserId, decimal Amount, string? Description) : ICommand<TransactionReceiptDto>;

internal sealed class WithdrawFundsCommandValidator : AbstractValidator<WithdrawFundsCommand>
{
    public WithdrawFundsCommandValidator()
    {
        RuleFor(c => c.Amount).GreaterThan(0).LessThanOrEqualTo(Money.MaxSingleTransaction).PrecisionScale(12, 2, true);
        RuleFor(c => c.Description).MaximumLength(140);
    }
}

internal sealed class WithdrawFundsCommandHandler(
    IWalletRepository wallets,
    IUnitOfWork unitOfWork,
    IIntegrationEventPublisher publisher,
    ICacheService cache,
    TimeProvider clock) : ICommandHandler<WithdrawFundsCommand, TransactionReceiptDto>
{
    public async Task<Result<TransactionReceiptDto>> Handle(WithdrawFundsCommand command, CancellationToken cancellationToken)
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

        var now = clock.GetUtcNow().UtcDateTime;
        var entry = wallet.Withdraw(money.Value, command.Description, now);

        if (entry.IsFailure)
        {
            if (entry.Error == WalletErrors.InsufficientFunds)
            {
                // A bounced withdrawal is a credit signal too: record it through the outbox.
                await publisher.PublishAsync(
                    new TransactionDeclinedIntegrationEvent(
                        Guid.NewGuid(), wallet.OwnerId, wallet.Id, TransactionKind.Withdrawal,
                        command.Amount, wallet.Balance, entry.Error.Code, now),
                    cancellationToken);
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }

            return entry.Error;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        await cache.RemoveAsync(WalletCacheKeys.Summary(command.UserId), cancellationToken);

        return entry.Value.ToReceipt(wallet.Currency);
    }
}
