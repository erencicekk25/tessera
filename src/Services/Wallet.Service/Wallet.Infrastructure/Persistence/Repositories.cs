using Microsoft.EntityFrameworkCore;
using Wallet.Domain.Abstractions;
using Wallet.Domain.Users;
using Wallet.Domain.Wallets;

namespace Wallet.Infrastructure.Persistence;

internal sealed class UserAccountRepository(WalletDbContext db) : IUserAccountRepository
{
    public Task<UserAccount?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        db.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    public Task<UserAccount?> GetByEmailAsync(EmailAddress email, CancellationToken cancellationToken) =>
        db.Users.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

    public Task<bool> ExistsAsync(EmailAddress email, CancellationToken cancellationToken) =>
        db.Users.AnyAsync(u => u.Email == email, cancellationToken);

    public void Add(UserAccount user) => db.Users.Add(user);
}

internal sealed class WalletRepository(WalletDbContext db) : IWalletRepository
{
    // Historic ledger entries are deliberately not included: the aggregate only needs its balance.
    public Task<DigitalWallet?> GetByOwnerAsync(Guid ownerId, CancellationToken cancellationToken) =>
        db.Wallets.FirstOrDefaultAsync(w => w.OwnerId == ownerId, cancellationToken);

    public void Add(DigitalWallet wallet) => db.Wallets.Add(wallet);
}
