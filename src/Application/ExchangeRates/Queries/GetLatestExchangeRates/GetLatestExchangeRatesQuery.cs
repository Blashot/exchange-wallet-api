using Application.Abstractions.Messaging;

namespace Application.ExchangeRates.Queries.GetLatestExchangeRates;

public sealed record GetLatestExchangeRatesQuery : IQuery<ExchangeRatesResponse>;

