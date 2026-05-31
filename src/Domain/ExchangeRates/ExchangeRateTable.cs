using Domain.ExchangeRates.Entities;
using Domain.ExchangeRates.Events;
using Domain.Shared;
using SharedKernel;

namespace Domain.ExchangeRates;

public sealed class ExchangeRateTable : Entity
{
    private readonly List<ExchangeRate> _rates = [];

    public Guid Id { get; private set; }

    /// <summary>NBP table identifier, e.g. "001/B/NBP/2026".</summary>
    public string TableNumber { get; private set; }

    public DateOnly PublicationDate { get; private set; }

    public DateTime ImportedAt { get; private set; }

    public IReadOnlyList<ExchangeRate> Rates => _rates.AsReadOnly();

    // Required by EF Core.
    private ExchangeRateTable()
    {
        TableNumber = string.Empty;
    }
    
    
    public static ExchangeRateTable Create(
        string tableNumber,
        DateOnly publicationDate,
        DateTime importedAt,
        IEnumerable<(CurrencyCode CurrencyCode, string CurrencyName, decimal MidRate)> rates)
    {
        var table = new ExchangeRateTable
        {
            Id = Guid.NewGuid(),
            TableNumber = tableNumber,
            PublicationDate = publicationDate,
            ImportedAt = importedAt
        };

        foreach ((CurrencyCode code, string name, decimal midRate) in rates)
        {
            table._rates.Add(ExchangeRate.Create(table.Id, code, name, midRate));
        }

        table.Raise(new ExchangeRateTableImportedDomainEvent(table.Id, publicationDate));

        return table;
    }


    public Result<decimal> GetMidRate(CurrencyCode currency)
    {
        if (currency == CurrencyCode.PLN)
        {
            return Result.Success(1.0m);
        }

        ExchangeRate? rate = _rates.SingleOrDefault(r => r.CurrencyCode == currency);

        if (rate is null)
        {
            return Result.Failure<decimal>(ExchangeRateErrors.CurrencyNotFound(currency));
        }

        return Result.Success(rate.MidRate);
    }
}

