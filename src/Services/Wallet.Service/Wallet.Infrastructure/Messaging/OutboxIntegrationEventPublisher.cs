using MassTransit;
using Wallet.Application.Abstractions;

namespace Wallet.Infrastructure.Messaging;

/// <summary>
/// With the MassTransit bus outbox enabled, the scoped <see cref="IPublishEndpoint"/> does not hit
/// RabbitMQ directly: it adds an <c>OutboxMessage</c> to the scoped <c>WalletDbContext</c>. A background
/// delivery service relays committed rows to the broker (at-least-once).
/// </summary>
internal sealed class OutboxIntegrationEventPublisher(IPublishEndpoint publishEndpoint) : IIntegrationEventPublisher
{
    public Task PublishAsync<TEvent>(TEvent integrationEvent, CancellationToken cancellationToken = default)
        where TEvent : class =>
        publishEndpoint.Publish(integrationEvent, cancellationToken);
}
