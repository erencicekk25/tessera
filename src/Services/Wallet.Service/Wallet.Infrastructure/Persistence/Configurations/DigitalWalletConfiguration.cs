using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wallet.Domain.Users;
using Wallet.Domain.Wallets;

namespace Wallet.Infrastructure.Persistence.Configurations;

internal sealed class DigitalWalletConfiguration : IEntityTypeConfiguration<DigitalWallet>
{
    public void Configure(EntityTypeBuilder<DigitalWallet> builder)
    {
        builder.ToTable("wallets", t => t.HasCheckConstraint("ck_wallets_balance_non_negative", "balance >= 0"));
        builder.HasKey(w => w.Id);
        builder.Property(w => w.Id).ValueGeneratedNever();

        builder.Property(w => w.OwnerId).IsRequired();
        builder.HasIndex(w => w.OwnerId).IsUnique();
        builder.HasOne<UserAccount>().WithOne().HasForeignKey<DigitalWallet>(w => w.OwnerId).OnDelete(DeleteBehavior.Restrict);

        builder.Property(w => w.Currency).HasMaxLength(3).IsFixedLength().IsRequired();
        builder.Property(w => w.Balance).HasPrecision(18, 2);
        builder.Property(w => w.OpenedAtUtc).IsRequired();

        // PostgreSQL system column xmin changes on every UPDATE: free optimistic concurrency.
        builder.Property(w => w.Version)
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();

        builder.HasMany(w => w.Entries)
            .WithOne()
            .HasForeignKey(e => e.WalletId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Navigation(w => w.Entries).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Ignore(w => w.DomainEvents);
    }
}

internal sealed class LedgerEntryConfiguration : IEntityTypeConfiguration<LedgerEntry>
{
    public void Configure(EntityTypeBuilder<LedgerEntry> builder)
    {
        builder.ToTable("ledger_entries", t => t.HasCheckConstraint("ck_ledger_entries_amount_positive", "amount > 0"));
        builder.HasKey(e => e.Id);

        // Keys are assigned by the domain; EF must INSERT entries discovered through the aggregate.
        builder.Property(e => e.Id).ValueGeneratedNever();

        builder.Property(e => e.Type).HasConversion<string>().HasMaxLength(16);
        builder.Property(e => e.Direction).HasConversion<string>().HasMaxLength(8);
        builder.Property(e => e.Amount).HasPrecision(18, 2);
        builder.Property(e => e.BalanceAfter).HasPrecision(18, 2);
        builder.Property(e => e.Description).HasMaxLength(140).IsRequired();
        builder.Property(e => e.Reference).HasMaxLength(16).IsRequired();
        builder.Property(e => e.Counterparty).HasMaxLength(80);

        builder.HasIndex(e => e.Reference).IsUnique();
        builder.HasIndex(e => new { e.WalletId, e.PostedAtUtc }).IsDescending(false, true);
        builder.HasIndex(e => e.CorrelationId);
    }
}
