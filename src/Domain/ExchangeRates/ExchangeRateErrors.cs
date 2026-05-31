using Domain.Shared;
using SharedKernel;

namespace Domain.ExchangeRates;

public static class ExchangeRateErrors
{
    public static Error CurrencyNotFound(CurrencyCode currency) => Error.NotFound(
        "ExchangeRates.CurrencyNotFound",
        $"No exchange rate found for currency '{currency.Value}'.");
}

