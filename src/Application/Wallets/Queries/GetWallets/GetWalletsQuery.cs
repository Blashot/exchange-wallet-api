using Application.Abstractions.Messaging;

namespace Application.Wallets.Queries.GetWallets;

public sealed record GetWalletsQuery(Guid UserId) : IQuery<List<WalletSummaryResponse>>;

