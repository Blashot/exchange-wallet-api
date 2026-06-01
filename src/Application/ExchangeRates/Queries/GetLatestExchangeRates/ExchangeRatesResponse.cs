namespace Application.ExchangeRates.Queries.GetLatestExchangeRates;

public sealed class ExchangeRatesResponse
{
    public Guid Id { get; set; }
    public string TableNumber { get; set; }
    public DateOnly PublicationDate { get; set; }
    public DateTime ImportedAt { get; set; }
    public List<ExchangeRateItemResponse> Rates { get; set; } = [];
}

public sealed class ExchangeRateItemResponse
{
    public string CurrencyCode { get; set; }
    public string CurrencyName { get; set; }
    public decimal MidRate { get; set; }
}

