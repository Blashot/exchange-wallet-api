using Domain.Shared;
using SharedKernel;

namespace Domain.Wallets.Events;

public sealed record MoneyDepositedDomainEvent(
    Guid WalletId,
    CurrencyCode Currency,
    decimal Amount) : IDomainEvent;

