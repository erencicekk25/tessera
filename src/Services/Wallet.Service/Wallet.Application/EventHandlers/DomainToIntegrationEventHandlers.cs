using MediatR;
using Tessera.Contracts;
using Tessera.Application.Messaging;
using Wallet.Application.Abstractions;
using Wallet.Domain.Users;
using Wallet.Domain.Wallets;

namespace Wallet.Application.EventHandlers;

/// <summary>
/// Translates internal domain events into the public integration contract. Runs inside
/// <c>SaveChangesAsync</c> before the commit, so the outbox row shares the business transaction.
/// </summary>
internal sealed class LedgerEntryPostedDomainEventHandler(IIntegrationEventPublisher publisher)
    : INotificationHandler<LedgerEntryPostedDomainEvent>
{
    public Task Handle(LedgerEntryPostedDomainEvent notification, CancellationToken cancellationToken) =>
        publisher.PublishAsync(
            new TransactionCompletedIntegrationEvent(
                notification.EventId,
                notification.OwnerId,
                notification.WalletId,
                notification.LedgerEntryId,
                (TransactionKind)(int)notification.Type,
                notification.Amount,
                notification.BalanceAfter,
                notification.Currency,
                notification.OccurredAtUtc),
            cancellationToken);
}

internal sealed class UserRegisteredDomainEventHandler(IIntegrationEventPublisher publisher)
    : INotificationHandler<UserRegisteredDomainEvent>
{
    public Task Handle(UserRegisteredDomainEvent notification, CancellationToken cancellationToken) =>
        publisher.PublishAsync(
            new UserRegisteredIntegrationEvent(notification.EventId, notification.UserId, notification.FullName, notification.OccurredAtUtc),
            cancellationToken);
}
