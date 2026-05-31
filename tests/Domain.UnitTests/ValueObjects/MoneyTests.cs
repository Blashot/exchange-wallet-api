using Domain.Shared;
using Domain.Wallets.ValueObjects;

namespace Domain.UnitTests.ValueObjects;

public sealed class MoneyTests
{
    private static readonly CurrencyCode Usd = new("USD");
    private static readonly CurrencyCode Pln = CurrencyCode.PLN;
    

    [Fact]
    public void Constructor_ValidPositiveAmount_ShouldSucceed()
    {
        Money money = new(100m, Usd);

        money.Amount.ShouldBe(100m);
        money.Currency.ShouldBe(Usd);
    }

    [Fact]
    public void Constructor_ZeroAmount_ShouldSucceed()
    {
        Money money = new(0m, Pln);

        money.Amount.ShouldBe(0m);
    }

    [Fact]
    public void Constructor_VeryLargeAmount_ShouldSucceed()
    {
        Money money = new(decimal.MaxValue, Pln);

        money.Amount.ShouldBe(decimal.MaxValue);
    }

    [Fact]
    public void Amount_IsDecimalType_NotDoubleOrFloat()
    {
        Money money = new(1.005m, Usd);
        money.Amount.ShouldBe(1.005m);
        money.Amount.GetType().ShouldBe(typeof(decimal));
    }

    [Fact]
    public void Amount_DecimalPrecision_IsPreservedExactly()
    {
        decimal a = 0.1m;
        decimal b = 0.2m;
        Money money = new(a + b, Pln);

        money.Amount.ShouldBe(0.3m);
    }
    
    [Theory]
    [InlineData(-0.01)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Constructor_NegativeAmount_ShouldThrowArgumentOutOfRangeException(double rawAmount)
    {
        decimal amount = (decimal)rawAmount;

        Action act = () => _ = new Money(amount, Pln);

        act.ShouldThrow<ArgumentOutOfRangeException>();
    }
    
    [Fact]
    public void Constructor_NullCurrency_ShouldThrowArgumentNullException()
    {
        Action act = () => _ = new Money(100m, null!);

        act.ShouldThrow<ArgumentNullException>();
    }
    

    [Fact]
    public void Equality_SameAmountAndCurrency_ShouldBeEqual()
    {
        Money a = new(100m, Usd);
        Money b = new(100m, Usd);

        a.ShouldBe(b);
        (a == b).ShouldBeTrue();
    }

    [Fact]
    public void Equality_DifferentAmount_ShouldNotBeEqual()
    {
        Money a = new(100m, Usd);
        Money b = new(200m, Usd);

        a.ShouldNotBe(b);
    }

    [Fact]
    public void Equality_DifferentCurrency_ShouldNotBeEqual()
    {
        Money a = new(100m, Usd);
        Money b = new(100m, Pln);

        a.ShouldNotBe(b);
    }
    
    [Fact]
    public void ToString_ShouldFormatAmountAndCurrency()
    {
        Money money = new(42.5m, Usd);

        money.ToString().ShouldBe("42.5 USD");
    }
}

