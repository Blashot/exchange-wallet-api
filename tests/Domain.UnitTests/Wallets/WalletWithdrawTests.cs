using Domain.Shared;
using Domain.Wallets;
using Domain.Wallets.Entities;
using Domain.Wallets.Enums;
using Domain.Wallets.Events;
using SharedKernel;

namespace Domain.UnitTests.Wallets;

public sealed class WalletWithdrawTests
{
    private static readonly DateTime Now = DateTime.UtcNow;
    private static readonly Guid UserId = Guid.NewGuid();

    private static Wallet CreateFundedWallet(decimal initialBalance, CurrencyCode? currency = null)
    {
        Wallet wallet = Wallet.Create(UserId, "Test Wallet", Now).Value;
        wallet.Deposit(currency ?? CurrencyCode.PLN, initialBalance, Now);
        wallet.ClearDomainEvents();
        return wallet;
    }

    [Fact]
    public void Withdraw_ValidAmount_ShouldSucceed()
    {
        Wallet wallet = CreateFundedWallet(100m);

        Result result = wallet.Withdraw(CurrencyCode.PLN, 50m, Now);

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void Withdraw_ShouldDecreaseBalance()
    {
        Wallet wallet = CreateFundedWallet(100m);

        wallet.Withdraw(CurrencyCode.PLN, 60m, Now);

        wallet.Balances.Single(b => b.CurrencyCode == CurrencyCode.PLN).Amount.ShouldBe(40m);
    }

    [Fact]
    public void Withdraw_ExactBalance_ShouldReduceBalanceToZero()
    {
        Wallet wallet = CreateFundedWallet(100m);

        Result result = wallet.Withdraw(CurrencyCode.PLN, 100m, Now);

        result.IsSuccess.ShouldBeTrue();
        wallet.Balances.Single(b => b.CurrencyCode == CurrencyCode.PLN).Amount.ShouldBe(0m);
    }

    [Fact]
    public void Withdraw_MoreThanBalance_ShouldReturnInsufficientFunds()
    {
        Wallet wallet = CreateFundedWallet(100m);

        Result result = wallet.Withdraw(CurrencyCode.PLN, 100.01m, Now);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(WalletErrors.InsufficientFunds);
    }

    [Fact]
    public void Withdraw_MoreThanBalance_ShouldNotAllowNegativeBalance()
    {
        Wallet wallet = CreateFundedWallet(100m);

        wallet.Withdraw(CurrencyCode.PLN, 150m, Now);

        // Balance must never go below zero.
        wallet.Balances.Single(b => b.CurrencyCode == CurrencyCode.PLN).Amount.ShouldBeGreaterThanOrEqualTo(0m);
    }

    [Fact]
    public void Withdraw_NoCurrencyBalance_ShouldReturnInsufficientFunds()
    {
        Wallet wallet = Wallet.Create(UserId, "Empty Wallet", Now).Value;
        CurrencyCode usd = new("USD");

        Result result = wallet.Withdraw(usd, 10m, Now);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(WalletErrors.InsufficientFunds);
    }

    [Fact]
    public void Withdraw_NegativeAmount_ShouldReturnInvalidAmountError()
    {
        Wallet wallet = CreateFundedWallet(100m);

        Result result = wallet.Withdraw(CurrencyCode.PLN, -5m, Now);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(WalletErrors.InvalidAmount);
    }

    [Fact]
    public void Withdraw_ZeroAmount_ShouldReturnInvalidAmountError()
    {
        Wallet wallet = CreateFundedWallet(100m);

        Result result = wallet.Withdraw(CurrencyCode.PLN, 0m, Now);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(WalletErrors.InvalidAmount);
    }

    [Fact]
    public void Withdraw_ShouldNotChangeBalance_WhenAmountIsInvalid()
    {
        Wallet wallet = CreateFundedWallet(100m);

        wallet.Withdraw(CurrencyCode.PLN, -1m, Now);

        wallet.Balances.Single(b => b.CurrencyCode == CurrencyCode.PLN).Amount.ShouldBe(100m);
    }

    [Fact]
    public void Withdraw_ShouldAddWithdrawalTransactionToHistory()
    {
        Wallet wallet = CreateFundedWallet(200m);

        wallet.Withdraw(CurrencyCode.PLN, 75m, Now);

        WalletTransaction tx = wallet.Transactions
            .Where(t => t.Type == TransactionType.Withdrawal)
            .ShouldHaveSingleItem();
        tx.CurrencyCode.ShouldBe(CurrencyCode.PLN);
        tx.Amount.ShouldBe(75m);
        tx.OccurredAt.ShouldBe(Now);
    }

    [Theory]
    [InlineData(100, 100, 0)]
    [InlineData(100, 1, 99)]
    [InlineData(500, 499.99, 0.01)]
    public void Withdraw_SequentialWithdrawals_ShouldCumulativelyReduceBalance(
        decimal initial, decimal first, decimal expectedAfterFirst)
    {
        Wallet wallet = CreateFundedWallet(initial);

        wallet.Withdraw(CurrencyCode.PLN, first, Now);

        wallet.Balances.Single(b => b.CurrencyCode == CurrencyCode.PLN)
            .Amount.ShouldBe(expectedAfterFirst);
    }
    
    [Fact]
    public void Withdraw_ShouldRaiseMoneyWithdrawnDomainEvent()
    {
        Wallet wallet = CreateFundedWallet(100m);

        wallet.Withdraw(CurrencyCode.PLN, 40m, Now);

        MoneyWithdrawnDomainEvent evt = wallet.DomainEvents
            .OfType<MoneyWithdrawnDomainEvent>()
            .ShouldHaveSingleItem();
        evt.WalletId.ShouldBe(wallet.Id);
        evt.Currency.ShouldBe(CurrencyCode.PLN);
        evt.Amount.ShouldBe(40m);
    }

    [Fact]
    public void Withdraw_FailedAttempt_ShouldNotRaiseDomainEvent()
    {
        Wallet wallet = CreateFundedWallet(50m);

        wallet.Withdraw(CurrencyCode.PLN, 100m, Now);

        wallet.DomainEvents
            .OfType<MoneyWithdrawnDomainEvent>()
            .ShouldBeEmpty();
    }
}

