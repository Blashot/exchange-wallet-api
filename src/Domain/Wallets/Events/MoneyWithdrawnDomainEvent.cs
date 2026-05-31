using Domain.Shared;
using SharedKernel;

namespace Domain.Wallets.Events;

public sealed record MoneyWithdrawnDomainEvent(
    Guid WalletId,
    CurrencyCode Currency,
    decimal Amount) : IDomainEvent;

