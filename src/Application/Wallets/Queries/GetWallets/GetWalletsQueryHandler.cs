using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Users;
using Domain.Wallets;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Wallets.Queries.GetWallets;

internal sealed class GetWalletsQueryHandler(
    IApplicationDbContext context,
    IUserContext userContext)
    : IQueryHandler<GetWalletsQuery, List<WalletSummaryResponse>>
{
    public async Task<Result<List<WalletSummaryResponse>>> Handle(
        GetWalletsQuery query,
        CancellationToken cancellationToken)
    {
        if (query.UserId != userContext.UserId)
        {
            return Result.Failure<List<WalletSummaryResponse>>(UserErrors.Unauthorized());
        }

        List<Wallet> wallets = await context.Wallets
            .Include(w => w.Balances)
            .AsNoTracking()
            .Where(w => w.UserId == query.UserId)
            .ToListAsync(cancellationToken);

        var response = wallets
            .Select(w => new WalletSummaryResponse
            {
                Id = w.Id,
                Name = w.Name,
                CreatedAt = w.CreatedAt,
                Balances = w.Balances
                    .Select(b => new BalanceSummaryResponse
                    {
                        CurrencyCode = b.CurrencyCode.Value,
                        Amount = b.Amount
                    })
                    .ToList()
            })
            .ToList();

        return response;
    }
}



