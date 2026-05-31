using SharedKernel;

namespace Domain.Wallets.Events;

public sealed record WalletCreatedDomainEvent(Guid WalletId) : IDomainEvent;

