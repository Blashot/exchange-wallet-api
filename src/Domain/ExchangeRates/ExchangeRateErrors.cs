using Domain.Shared;
using SharedKernel;

namespace Domain.ExchangeRates;

public static class ExchangeRateErrors
{
    public static Error CurrencyNotFound(CurrencyCode currency) => Error.NotFound(
        "ExchangeRates.CurrencyNotFound",
        $"No exchange rate found for currency '{currency.Value}'.");
    
    
    public static readonly Error TableAlreadyImported = Error.Conflict(
        "ExchangeRates.TableAlreadyImported",
        "An exchange rate table for this publication date has already been imported.");

    public static readonly Error NoRatesAvailable = Error.NotFound(
        "ExchangeRates.NoRatesAvailable",
        "No exchange rate table is currently available.");

    public static Error NotFound(Guid id) => Error.NotFound(
        "ExchangeRates.NotFound",
        $"The exchange rate table with Id = '{id}' was not found.");
}

