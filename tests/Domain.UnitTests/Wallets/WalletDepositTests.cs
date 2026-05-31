using Domain.Shared;
using Domain.Wallets;
using Domain.Wallets.Entities;
using Domain.Wallets.Enums;
using SharedKernel;

namespace Domain.UnitTests.Wallets;

public sealed class WalletDepositTests
{
    private static readonly DateTime Now = DateTime.UtcNow;
    private static readonly Guid UserId = Guid.NewGuid();

    private static Wallet CreateWallet() =>
        Wallet.Create(UserId, "Test Wallet", Now).Value;

    [Fact]
    public void Deposit_ValidAmount_ShouldSucceed()
    {
        Wallet wallet = CreateWallet();

        Result result = wallet.Deposit(CurrencyCode.PLN, 100m, Now);

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void Deposit_ShouldCreateBalance_WhenCurrencySeenForFirstTime()
    {
        Wallet wallet = CreateWallet();

        wallet.Deposit(CurrencyCode.PLN, 100m, Now);

        wallet.Balances.Count.ShouldBe(1);
        wallet.Balances[0].CurrencyCode.ShouldBe(CurrencyCode.PLN);
        wallet.Balances[0].Amount.ShouldBe(100m);
    }

    [Fact]
    public void Deposit_ShouldAddToExistingBalance()
    {
        Wallet wallet = CreateWallet();
        wallet.Deposit(CurrencyCode.PLN, 100m, Now);

        wallet.Deposit(CurrencyCode.PLN, 50m, Now);

        wallet.Balances.Single(b => b.CurrencyCode == CurrencyCode.PLN).Amount.ShouldBe(150m);
    }

    [Fact]
    public void Deposit_NegativeAmount_ShouldReturnInvalidAmountError()
    {
        Wallet wallet = CreateWallet();

        Result result = wallet.Deposit(CurrencyCode.PLN, -1m, Now);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(WalletErrors.InvalidAmount);
    }

    [Fact]
    public void Deposit_ZeroAmount_ShouldReturnInvalidAmountError()
    {
        Wallet wallet = CreateWallet();

        Result result = wallet.Deposit(CurrencyCode.PLN, 0m, Now);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(WalletErrors.InvalidAmount);
    }

    [Fact]
    public void Deposit_ShouldNotChangeBalance_WhenAmountIsInvalid()
    {
        Wallet wallet = CreateWallet();
        wallet.Deposit(CurrencyCode.PLN, 100m, Now);

        wallet.Deposit(CurrencyCode.PLN, 0m, Now);

        wallet.Balances.Single(b => b.CurrencyCode == CurrencyCode.PLN).Amount.ShouldBe(100m);
    }

    [Fact]
    public void Deposit_ShouldAddTransactionToHistory()
    {
        Wallet wallet = CreateWallet();

        wallet.Deposit(CurrencyCode.PLN, 123.45m, Now);

        WalletTransaction tx = wallet.Transactions.ShouldHaveSingleItem();
        tx.Type.ShouldBe(TransactionType.Deposit);
        tx.CurrencyCode.ShouldBe(CurrencyCode.PLN);
        tx.Amount.ShouldBe(123.45m);
        tx.OccurredAt.ShouldBe(Now);
    }
    

    [Fact]
    public void Deposit_MultipleCurrencies_ShouldMaintainSeparateBalances()
    {
        Wallet wallet = CreateWallet();
        CurrencyCode usd = new("USD");

        wallet.Deposit(CurrencyCode.PLN, 200m, Now);
        wallet.Deposit(usd, 50m, Now);

        wallet.Balances.Count.ShouldBe(2);
        wallet.Balances.Single(b => b.CurrencyCode == CurrencyCode.PLN).Amount.ShouldBe(200m);
        wallet.Balances.Single(b => b.CurrencyCode == usd).Amount.ShouldBe(50m);
    }
}

