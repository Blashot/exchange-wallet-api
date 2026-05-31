using Domain.Shared;
using SharedKernel;

namespace Domain.ExchangeRates.Entities;

public sealed class ExchangeRate : Entity
{
    public Guid Id { get; private set; }

    public Guid ExchangeRateTableId { get; private set; }

    public CurrencyCode CurrencyCode { get; private set; }

    public string CurrencyName { get; private set; }

    /// <summary>
    /// Number of PLN equivalent to 1 unit of this currency.
    /// Example: USD=3.95 means 1 USD = 3.95 PLN.
    /// </summary>
    public decimal MidRate { get; private set; }
    
#pragma warning disable CS8618
    private ExchangeRate() { }
#pragma warning restore CS8618

    internal static ExchangeRate Create(
        Guid exchangeRateTableId,
        CurrencyCode currencyCode,
        string currencyName,
        decimal midRate)
    {
        if (exchangeRateTableId == Guid.Empty)
        {
            throw new ArgumentException("Exchange rate table ID cannot be empty.", nameof(exchangeRateTableId));
        }

        if (string.IsNullOrWhiteSpace(currencyName))
        {
            throw new ArgumentException("Currency name cannot be empty.", nameof(currencyName));
        }

        if (midRate <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(midRate), "Mid rate must be greater than zero.");
        }

        return new ExchangeRate
        {
            Id = Guid.NewGuid(),
            ExchangeRateTableId = exchangeRateTableId,
            CurrencyCode = currencyCode,
            CurrencyName = currencyName,
            MidRate = midRate
        };
    }
}

