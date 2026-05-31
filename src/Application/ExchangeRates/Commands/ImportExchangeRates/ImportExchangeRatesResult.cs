namespace Application.ExchangeRates.Commands.ImportExchangeRates;


public sealed record ImportExchangeRatesResult(
    int TablesImported,
    int RatesImported);

