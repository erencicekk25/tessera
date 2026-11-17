using Wallet.Domain.Users;
using Wallet.Domain.Wallets;

namespace Wallet.Domain.Abstractions;

public interface IUserAccountRepository
{
    Task<UserAccount?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<UserAccount?> GetByEmailAsync(EmailAddress email, CancellationToken cancellationToken);

    Task<bool> ExistsAsync(EmailAddress email, CancellationToken cancellationToken);

    void Add(UserAccount user);
}

public interface IWalletRepository
{
    Task<DigitalWallet?> GetByOwnerAsync(Guid ownerId, CancellationToken cancellationToken);

    void Add(DigitalWallet wallet);
}
