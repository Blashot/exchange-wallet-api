using Application.Abstractions.Messaging;

namespace Application.Wallets.Commands.CreateWallet;

public sealed record CreateWalletCommand(Guid UserId, string Name) : ICommand<Guid>;

