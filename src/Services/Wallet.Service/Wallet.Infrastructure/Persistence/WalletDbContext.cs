using MassTransit;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Tessera.SharedKernel.Domain;
using Wallet.Application.Abstractions;
using Wallet.Domain.Users;
using Wallet.Domain.Wallets;

namespace Wallet.Infrastructure.Persistence;

public sealed class WalletDbContext(DbContextOptions<WalletDbContext> options, IPublisher publisher)
    : DbContext(options), IUnitOfWork
{
    public const string Schema = "wallet";

    public DbSet<UserAccount> Users => Set<UserAccount>();

    public DbSet<DigitalWallet> Wallets => Set<DigitalWallet>();

    public DbSet<LedgerEntry> LedgerEntries => Set<LedgerEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(WalletDbContext).Assembly);

        // MassTransit transactional outbox tables live next to the business data.
        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();
    }

    /// <summary>
    /// Dispatches domain events <b>before</b> committing. Handlers translate them into integration
    /// events that MassTransit writes to the outbox tables tracked by this same context, so business
    /// state and outgoing messages are persisted atomically.
    /// </summary>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        while (true)
        {
            var aggregates = ChangeTracker.Entries<IHasDomainEvents>()
                .Select(e => e.Entity)
                .Where(e => e.DomainEvents.Count > 0)
                .ToList();

            if (aggregates.Count == 0)
            {
                break;
            }

            var events = aggregates.SelectMany(a => a.DomainEvents).ToList();
            aggregates.ForEach(a => a.ClearDomainEvents());

            foreach (var domainEvent in events)
            {
                await publisher.Publish(domainEvent, cancellationToken);
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}
