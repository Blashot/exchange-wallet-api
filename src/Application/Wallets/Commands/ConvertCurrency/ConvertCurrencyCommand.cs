using Application.Abstractions.Messaging;

namespace Application.Wallets.Commands.ConvertCurrency;

public sealed record ConvertCurrencyCommand(
    Guid WalletId,
    Guid UserId,
    string FromCurrencyCode,
    string ToCurrencyCode,
    decimal Amount) : ICommand;

