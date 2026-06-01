using Domain.Shared;
using Domain.Wallets;
using Domain.Wallets.Entities;
using Domain.Wallets.Enums;
using Domain.Wallets.Events;

namespace Application.UnitTests.Wallets;


public sealed class WalletConvertTests
{
    private static readonly DateTime Now = DateTime.UtcNow;
    private static readonly Guid UserId = Guid.NewGuid();

    private static readonly CurrencyCode Usd = new("USD");
    private static readonly CurrencyCode Eur = new("EUR");


    private const decimal UsdRate = 4.00m;  // 1 USD = 4.00 PLN
    private const decimal EurRate = 4.50m;  // 1 EUR = 4.50 PLN

    private static Wallet CreateWalletWithBalance(CurrencyCode currency, decimal amount)
    {
        Wallet wallet = Wallet.Create(UserId, "Test Wallet", Now).Value;
        wallet.Deposit(currency, amount, Now);
        wallet.ClearDomainEvents();
        return wallet;
    }
    

    [Fact]
    public void Convert_ValidInputs_ShouldSucceed()
    {
        Wallet wallet = CreateWalletWithBalance(Usd, 100m);

        Result result = wallet.Convert(Usd, Eur, 100m, UsdRate, EurRate, Now);

        result.IsSuccess.ShouldBeTrue();
    }
    

    [Fact]
    public void Convert_ShouldCalculateTargetAmountUsingPlnAsPivot()
    {
        // This verifies the core conversion logic using PLN as pivot currency:
        // 100 USD → PLN → EUR
        // plnEquivalent = 100 * 4.00 = 400 PLN
        // eurReceived   = 400 / 4.50 ≈ 88.88888889 EUR
        Wallet wallet = CreateWalletWithBalance(Usd, 100m);

        wallet.Convert(Usd, Eur, 100m, UsdRate, EurRate, Now);

        decimal expectedEur = Math.Round(100m * UsdRate / EurRate, 8, MidpointRounding.AwayFromZero);
        wallet.Balances.Single(b => b.CurrencyCode == Eur).Amount.ShouldBe(expectedEur);
    }

    [Fact]
    public void Convert_FromForeignToPln_ShouldUseRateDirectly()
    {
        // 50 USD → PLN, toMidRate = 1.0 (PLN is pivot)
        // plnReceived = 50 * 4.00 / 1.0 = 200 PLN
        Wallet wallet = CreateWalletWithBalance(Usd, 50m);

        wallet.Convert(Usd, CurrencyCode.PLN, 50m, UsdRate, 1.0m, Now);

        wallet.Balances.Single(b => b.CurrencyCode == CurrencyCode.PLN).Amount.ShouldBe(200m);
    }

    [Fact]
    public void Convert_FromPlnToForeign_ShouldUseRateInversely()
    {
        // 400 PLN → USD, fromMidRate = 1.0, toMidRate = 4.00
        // usdReceived = 400 * 1.0 / 4.00 = 100 USD
        Wallet wallet = CreateWalletWithBalance(CurrencyCode.PLN, 400m);

        wallet.Convert(CurrencyCode.PLN, Usd, 400m, 1.0m, UsdRate, Now);

        wallet.Balances.Single(b => b.CurrencyCode == Usd).Amount.ShouldBe(100m);
    }

    [Fact]
    public void Convert_ShouldDeductFromSourceBalance()
    {
        Wallet wallet = CreateWalletWithBalance(Usd, 200m);

        wallet.Convert(Usd, Eur, 100m, UsdRate, EurRate, Now);

        wallet.Balances.Single(b => b.CurrencyCode == Usd).Amount.ShouldBe(100m);
    }

    [Fact]
    public void Convert_ShouldNotAllowNegativeSourceBalance()
    {
        Wallet wallet = CreateWalletWithBalance(Usd, 50m);

        Result result = wallet.Convert(Usd, Eur, 100m, UsdRate, EurRate, Now);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(WalletErrors.InsufficientFunds);
        // Source balance must never go below zero.
        wallet.Balances.Single(b => b.CurrencyCode == Usd).Amount.ShouldBeGreaterThanOrEqualTo(0m);
    }

    [Fact]
    public void Convert_SameCurrency_ShouldReturnSameCurrencyError()
    {
        Wallet wallet = CreateWalletWithBalance(Usd, 100m);

        Result result = wallet.Convert(Usd, Usd, 100m, UsdRate, UsdRate, Now);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(WalletErrors.SameCurrency);
    }

    [Fact]
    public void Convert_ZeroAmount_ShouldReturnInvalidAmountError()
    {
        Wallet wallet = CreateWalletWithBalance(Usd, 100m);

        Result result = wallet.Convert(Usd, Eur, 0m, UsdRate, EurRate, Now);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(WalletErrors.InvalidAmount);
    }

    [Fact]
    public void Convert_NegativeAmount_ShouldReturnInvalidAmountError()
    {
        Wallet wallet = CreateWalletWithBalance(Usd, 100m);

        Result result = wallet.Convert(Usd, Eur, -10m, UsdRate, EurRate, Now);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(WalletErrors.InvalidAmount);
    }

    [Fact]
    public void Convert_NoSourceBalance_ShouldReturnInsufficientFunds()
    {
        Wallet wallet = Wallet.Create(UserId, "Empty Wallet", Now).Value;

        Result result = wallet.Convert(Usd, Eur, 10m, UsdRate, EurRate, Now);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(WalletErrors.InsufficientFunds);
    }
    

    [Fact]
    public void Convert_ShouldStoreTwoTransactionLegsInHistory()
    {
        Wallet wallet = CreateWalletWithBalance(Usd, 100m);

        wallet.Convert(Usd, Eur, 100m, UsdRate, EurRate, Now);

        wallet.Transactions.Count(t =>
            t.Type is TransactionType.ConversionDebit or TransactionType.ConversionCredit)
            .ShouldBe(2);
    }

    [Fact]
    public void Convert_ShouldStoreDebitLegWithCorrectAmountAndCurrency()
    {
        Wallet wallet = CreateWalletWithBalance(Usd, 100m);

        wallet.Convert(Usd, Eur, 100m, UsdRate, EurRate, Now);

        WalletTransaction debit = wallet.Transactions
            .Single(t => t.Type == TransactionType.ConversionDebit);
        debit.CurrencyCode.ShouldBe(Usd);
        debit.Amount.ShouldBe(100m);
    }

    [Fact]
    public void Convert_ShouldStoreCreditLegWithExchangeRateDerivedAmount()
    {
        // This verifies that the stored credit amount reflects the applied exchange rates:
        // toAmount = fromAmount * fromMidRate / toMidRate  (PLN pivot)
        Wallet wallet = CreateWalletWithBalance(Usd, 100m);

        wallet.Convert(Usd, Eur, 100m, UsdRate, EurRate, Now);

        decimal expectedCredit = Math.Round(100m * UsdRate / EurRate, 8, MidpointRounding.AwayFromZero);
        WalletTransaction credit = wallet.Transactions
            .Single(t => t.Type == TransactionType.ConversionCredit);
        credit.CurrencyCode.ShouldBe(Eur);
        credit.Amount.ShouldBe(expectedCredit);
    }

    [Fact]
    public void Convert_ShouldStoreTransactionWithCorrectTimestamp()
    {
        Wallet wallet = CreateWalletWithBalance(Usd, 100m);
        DateTime operationTime = new(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc);

        wallet.Convert(Usd, Eur, 100m, UsdRate, EurRate, operationTime);

        wallet.Transactions
            .Where(t => t.Type is TransactionType.ConversionDebit or TransactionType.ConversionCredit)
            .ShouldAllBe(t => t.OccurredAt == operationTime);
    }

    [Fact]
    public void Convert_DifferentRates_ShouldProduceDifferentCreditAmounts()
    {
        // Changing the EUR rate changes the credit amount recorded in history,
        // proving the stored amounts are driven by the supplied rates.
        Wallet wallet1 = CreateWalletWithBalance(Usd, 100m);
        Wallet wallet2 = CreateWalletWithBalance(Usd, 100m);

        wallet1.Convert(Usd, Eur, 100m, UsdRate, 4.00m, Now);  // 1 EUR = 4.00 PLN
        wallet2.Convert(Usd, Eur, 100m, UsdRate, 5.00m, Now);  // 1 EUR = 5.00 PLN

        decimal credit1 = wallet1.Transactions.Single(t => t.Type == TransactionType.ConversionCredit).Amount;
        decimal credit2 = wallet2.Transactions.Single(t => t.Type == TransactionType.ConversionCredit).Amount;

        credit1.ShouldBeGreaterThan(credit2); // cheaper EUR → more EUR received
    }

    [Fact]
    public void Convert_ShouldRaiseCurrencyConvertedDomainEvent()
    {
        Wallet wallet = CreateWalletWithBalance(Usd, 100m);

        wallet.Convert(Usd, Eur, 100m, UsdRate, EurRate, Now);

        CurrencyConvertedDomainEvent evt = wallet.DomainEvents
            .OfType<CurrencyConvertedDomainEvent>()
            .ShouldHaveSingleItem();

        evt.WalletId.ShouldBe(wallet.Id);
        evt.FromCurrency.ShouldBe(Usd);
        evt.FromAmount.ShouldBe(100m);
        evt.ToCurrency.ShouldBe(Eur);
        evt.ToAmount.ShouldBe(Math.Round(100m * UsdRate / EurRate, 8, MidpointRounding.AwayFromZero));
    }

    [Fact]
    public void Convert_FailedAttempt_ShouldNotRaiseDomainEvent()
    {
        Wallet wallet = CreateWalletWithBalance(Usd, 10m);

        wallet.Convert(Usd, Eur, 100m, UsdRate, EurRate, Now);

        wallet.DomainEvents.OfType<CurrencyConvertedDomainEvent>().ShouldBeEmpty();
    }
}

