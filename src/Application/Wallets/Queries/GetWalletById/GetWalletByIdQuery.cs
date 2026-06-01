using Application.Abstractions.Messaging;

namespace Application.Wallets.Queries.GetWalletById;

public sealed record GetWalletByIdQuery(Guid WalletId, Guid UserId) : IQuery<WalletResponse>;

