using Application.Abstractions.Messaging;

namespace Application.Wallets.Commands.Withdraw;

public sealed record WithdrawCommand(
    Guid WalletId,
    Guid UserId,
    string CurrencyCode,
    decimal Amount) : ICommand;

