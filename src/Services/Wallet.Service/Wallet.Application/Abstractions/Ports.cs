using Wallet.Domain.Users;

namespace Wallet.Application.Abstractions;

/// <summary>Commits the current business transaction (aggregates + outbox messages) atomically.</summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

public interface IPasswordHasher
{
    string Hash(string password);

    bool Verify(string hashedPassword, string providedPassword);
}

public interface IAccessTokenIssuer
{
    AccessToken Issue(UserAccount user);
}

public sealed record AccessToken(string Value, DateTime ExpiresAtUtc);

public interface ICacheService
{
    Task<object?> GetAsync(string key, Type valueType, CancellationToken cancellationToken = default);

    Task SetAsync(string key, object value, Type valueType, TimeSpan? expiration = null, CancellationToken cancellationToken = default);

    Task RemoveAsync(string key, CancellationToken cancellationToken = default);
}

/// <summary>
/// Publishes integration events to other bounded contexts. Implementations must enlist in the
/// current unit of work (transactional outbox) so events are never lost or sent for rolled-back work.
/// </summary>
public interface IIntegrationEventPublisher
{
    Task PublishAsync<TEvent>(TEvent integrationEvent, CancellationToken cancellationToken = default)
        where TEvent : class;
}
