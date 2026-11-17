using Wallet.Domain.Wallets;

namespace Wallet.Domain.Tests;

public sealed class DigitalWalletTests
{
    private static readonly DateTime Now = new(2026, 11, 17, 9, 30, 0, DateTimeKind.Utc);

    private static Money Try(decimal amount) => Money.Create(amount, DigitalWallet.DefaultCurrency).Value;

    [Fact]
    public void Deposit_credits_balance_and_posts_a_ledger_entry()
    {
        var wallet = DigitalWallet.Open(Guid.NewGuid(), Now);

        var result = wallet.Deposit(Try(1_250.50m), "Salary", Now);

        Assert.True(result.IsSuccess);
        Assert.Equal(1_250.50m, wallet.Balance);
        var entry = Assert.Single(wallet.Entries);
        Assert.Equal(EntryDirection.Credit, entry.Direction);
        Assert.Equal(1_250.50m, entry.BalanceAfter);
        Assert.StartsWith("DEP-", entry.Reference);
        Assert.IsType<LedgerEntryPostedDomainEvent>(Assert.Single(wallet.DomainEvents));
    }

    [Fact]
    public void Withdraw_more_than_balance_is_rejected_and_leaves_state_untouched()
    {
        var wallet = DigitalWallet.Open(Guid.NewGuid(), Now);
        wallet.Deposit(Try(100m), null, Now);
        wallet.ClearDomainEvents();

        var result = wallet.Withdraw(Try(100.01m), null, Now);

        Assert.True(result.IsFailure);
        Assert.Equal(WalletErrors.InsufficientFunds, result.Error);
        Assert.Equal(100m, wallet.Balance);
        Assert.Single(wallet.Entries);
        Assert.Empty(wallet.DomainEvents);
    }

    [Fact]
    public void Balance_always_equals_credits_minus_debits()
    {
        var wallet = DigitalWallet.Open(Guid.NewGuid(), Now);
        wallet.Deposit(Try(500m), null, Now);
        wallet.Withdraw(Try(120m), null, Now);
        wallet.Deposit(Try(80.25m), null, Now);
        wallet.Withdraw(Try(10m), null, Now);

        var credits = wallet.Entries.Where(e => e.Direction == EntryDirection.Credit).Sum(e => e.Amount);
        var debits = wallet.Entries.Where(e => e.Direction == EntryDirection.Debit).Sum(e => e.Amount);

        Assert.Equal(credits - debits, wallet.Balance);
        Assert.Equal(wallet.Balance, wallet.Entries.Last().BalanceAfter);
    }

    [Fact]
    public void Transfer_posts_both_legs_with_a_shared_correlation_id()
    {
        var alice = DigitalWallet.Open(Guid.NewGuid(), Now);
        var bob = DigitalWallet.Open(Guid.NewGuid(), Now);
        alice.Deposit(Try(300m), null, Now);

        var result = TransferService.Transfer(alice, bob, Try(120m), "Alice", "Bob", "Dinner", Now);

        Assert.True(result.IsSuccess);
        Assert.Equal(180m, alice.Balance);
        Assert.Equal(120m, bob.Balance);
        Assert.Equal(result.Value.TransferId, result.Value.Outgoing.CorrelationId);
        Assert.Equal(result.Value.TransferId, result.Value.Incoming.CorrelationId);
        Assert.Equal("Bob", result.Value.Outgoing.Counterparty);
        Assert.Equal(LedgerEntryType.TransferIn, result.Value.Incoming.Type);
    }

    [Fact]
    public void Transfer_to_self_is_rejected()
    {
        var owner = Guid.NewGuid();
        var wallet = DigitalWallet.Open(owner, Now);
        wallet.Deposit(Try(50m), null, Now);

        var result = TransferService.Transfer(wallet, wallet, Try(10m), "Me", "Me", null, Now);

        Assert.Equal(WalletErrors.SelfTransfer, result.Error);
        Assert.Equal(50m, wallet.Balance);
    }

    [Fact]
    public void Insufficient_transfer_touches_neither_wallet()
    {
        var alice = DigitalWallet.Open(Guid.NewGuid(), Now);
        var bob = DigitalWallet.Open(Guid.NewGuid(), Now);

        var result = TransferService.Transfer(alice, bob, Try(1m), "Alice", "Bob", null, Now);

        Assert.Equal(WalletErrors.InsufficientFunds, result.Error);
        Assert.Empty(alice.Entries);
        Assert.Empty(bob.Entries);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(10.001)]
    [InlineData(250_000.01)]
    public void Money_rejects_invalid_amounts(decimal amount)
    {
        Assert.True(Money.Create(amount, "TRY").IsFailure);
    }
}
