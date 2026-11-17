using FluentValidation;
using Tessera.Contracts;
using Tessera.SharedKernel.Results;
using Tessera.Application.Messaging;
using Wallet.Application.Abstractions;
using Wallet.Domain.Abstractions;
using Wallet.Domain.Users;
using Wallet.Domain.Wallets;

namespace Wallet.Application.Features.Wallets;

public sealed record TransferFundsCommand(Guid UserId, string RecipientEmail, decimal Amount, string? Note)
    : ICommand<TransactionReceiptDto>;

internal sealed class TransferFundsCommandValidator : AbstractValidator<TransferFundsCommand>
{
    public TransferFundsCommandValidator()
    {
        RuleFor(c => c.RecipientEmail).NotEmpty().EmailAddress();
        RuleFor(c => c.Amount).GreaterThan(0).LessThanOrEqualTo(Money.MaxSingleTransaction).PrecisionScale(12, 2, true);
        RuleFor(c => c.Note).MaximumLength(140);
    }
}

/// <summary>
/// Moves money between two wallets. Both legs and both outbox messages are committed in one
/// database transaction, so the ledger can never show half a transfer.
/// </summary>
internal sealed class TransferFundsCommandHandler(
    IUserAccountRepository users,
    IWalletRepository wallets,
    IUnitOfWork unitOfWork,
    IIntegrationEventPublisher publisher,
    ICacheService cache,
    TimeProvider clock) : ICommandHandler<TransferFundsCommand, TransactionReceiptDto>
{
    public async Task<Result<TransactionReceiptDto>> Handle(TransferFundsCommand command, CancellationToken cancellationToken)
    {
        var sender = await users.GetByIdAsync(command.UserId, cancellationToken);
        var sourceWallet = await wallets.GetByOwnerAsync(command.UserId, cancellationToken);
        if (sender is null || sourceWallet is null)
        {
            return WalletErrors.NotFound;
        }

        var recipientEmail = EmailAddress.Create(command.RecipientEmail);
        if (recipientEmail.IsFailure)
        {
            return recipientEmail.Error;
        }

        var recipient = await users.GetByEmailAsync(recipientEmail.Value, cancellationToken);
        var destinationWallet = recipient is null ? null : await wallets.GetByOwnerAsync(recipient.Id, cancellationToken);
        if (recipient is null || destinationWallet is null)
        {
            return WalletErrors.RecipientNotFound;
        }

        var money = Money.Create(command.Amount, sourceWallet.Currency);
        if (money.IsFailure)
        {
            return money.Error;
        }

        var now = clock.GetUtcNow().UtcDateTime;
        var transfer = TransferService.Transfer(
            sourceWallet, destinationWallet, money.Value, sender.FullName, recipient.FullName, command.Note, now);

        if (transfer.IsFailure)
        {
            if (transfer.Error == WalletErrors.InsufficientFunds)
            {
                await publisher.PublishAsync(
                    new TransactionDeclinedIntegrationEvent(
                        Guid.NewGuid(), sourceWallet.OwnerId, sourceWallet.Id, TransactionKind.TransferOut,
                        command.Amount, sourceWallet.Balance, transfer.Error.Code, now),
                    cancellationToken);
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }

            return transfer.Error;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        await cache.RemoveAsync(WalletCacheKeys.Summary(sender.Id), cancellationToken);
        await cache.RemoveAsync(WalletCacheKeys.Summary(recipient.Id), cancellationToken);

        return transfer.Value.Outgoing.ToReceipt(sourceWallet.Currency);
    }
}
