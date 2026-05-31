using Application.Abstractions.Messaging;

namespace Application.ExchangeRates.Commands.ImportExchangeRates;


public sealed record ImportExchangeRatesCommand : ICommand<ImportExchangeRatesResult>;

