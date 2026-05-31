using Domain.ExchangeRates;
using Domain.Shared;
using SharedKernel;

namespace Domain.UnitTests.ExchangeRates;

public sealed class ExchangeRateTableTests
{
    private static readonly DateTime Now = DateTime.UtcNow;
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow);

    private static readonly CurrencyCode Usd = new("USD");
    private static readonly CurrencyCode Eur = new("EUR");
    private static readonly CurrencyCode Pln = CurrencyCode.PLN;

    private static ExchangeRateTable CreateTable(params (CurrencyCode Code, decimal Rate)[] rates)
    {
        IEnumerable<(CurrencyCode, string, decimal)> rateData =
            rates.Select(r => (r.Code, r.Code.Value + " Currency", r.Rate));

        return ExchangeRateTable.Create("001/B/NBP/2026", Today, Now, rateData);
    }


    

    [Fact]
    public void Create_ShouldSetTableNumber()
    {
        var table = ExchangeRateTable.Create("001/B/NBP/2026", Today, Now, []);

        table.TableNumber.ShouldBe("001/B/NBP/2026");
    }

    [Fact]
    public void Create_ShouldSetPublicationDate()
    {
        var table = ExchangeRateTable.Create("001/B/NBP/2026", Today, Now, []);

        table.PublicationDate.ShouldBe(Today);
    }

    [Fact]
    public void Create_ShouldSetImportedAt()
    {
        var table = ExchangeRateTable.Create("001/B/NBP/2026", Today, Now, []);

        table.ImportedAt.ShouldBe(Now);
    }

    [Fact]
    public void Create_ShouldPopulateRates()
    {
        ExchangeRateTable table = CreateTable((Usd, 4.00m), (Eur, 4.50m));

        table.Rates.Count.ShouldBe(2);
    }
    
    

    [Fact]
    public void GetMidRate_ForPLN_ShouldReturnOne()
    {
        ExchangeRateTable table = CreateTable((Usd, 4.00m));

        Result<decimal> result = table.GetMidRate(Pln);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(1.0m);
    }

    [Fact]
    public void GetMidRate_ForKnownCurrency_ShouldReturnCorrectRate()
    {
        ExchangeRateTable table = CreateTable((Usd, 4.00m), (Eur, 4.50m));

        Result<decimal> usdResult = table.GetMidRate(Usd);
        Result<decimal> eurResult = table.GetMidRate(Eur);

        usdResult.IsSuccess.ShouldBeTrue();
        usdResult.Value.ShouldBe(4.00m);

        eurResult.IsSuccess.ShouldBeTrue();
        eurResult.Value.ShouldBe(4.50m);
    }

    [Fact]
    public void GetMidRate_ForUnknownCurrency_ShouldReturnFailure()
    {
        ExchangeRateTable table = CreateTable((Usd, 4.00m));

        Result<decimal> result = table.GetMidRate(new CurrencyCode("CHF"));

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("ExchangeRates.CurrencyNotFound");
    }

    [Fact]
    public void GetMidRate_PLNShouldWork_EvenWhenNoRatesPresent()
    {
        var table = ExchangeRateTable.Create("001/B/NBP/2026", Today, Now, []);

        Result<decimal> result = table.GetMidRate(Pln);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(1.0m);
    }

    
    [Fact]
    public void GetMidRate_Rates_AllowPivotArithmetic_UsdToEur()
    {
        // 100 USD → PLN → EUR
        ExchangeRateTable table = CreateTable((Usd, 4.00m), (Eur, 4.50m));

        decimal usdRate = table.GetMidRate(Usd).Value;
        decimal eurRate = table.GetMidRate(Eur).Value;

        decimal received = Math.Round(100m * usdRate / eurRate, 8, MidpointRounding.AwayFromZero);

        received.ShouldBe(Math.Round(400m / 4.50m, 8, MidpointRounding.AwayFromZero));
    }
}
