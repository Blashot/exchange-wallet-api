using Domain.Shared;
using SharedKernel;

namespace Domain.Wallets.Events;

public sealed record CurrencyConvertedDomainEvent(
    Guid WalletId,
    CurrencyCode FromCurrency,
    decimal FromAmount,
    CurrencyCode ToCurrency,
    decimal ToAmount) : IDomainEvent;

