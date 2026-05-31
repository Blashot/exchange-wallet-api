namespace Application.Abstractions.ExchangeRates;


public sealed record NbpTableData(
    string TableNumber,
    DateOnly EffectiveDate,
    IReadOnlyList<NbpRateData> Rates);

public sealed record NbpRateData(
    string CurrencyName,
    string CurrencyCode,
    decimal MidRate);

