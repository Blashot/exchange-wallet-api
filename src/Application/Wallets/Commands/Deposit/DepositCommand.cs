using Application.Abstractions.Messaging;

namespace Application.Wallets.Commands.Deposit;

public sealed record DepositCommand(
    Guid WalletId,
    Guid UserId,
    string CurrencyCode,
    decimal Amount) : ICommand;

