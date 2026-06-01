using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Wallets;
using Domain.Wallets.Entities;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Wallets.Queries.GetWalletById;

internal sealed class GetWalletByIdQueryHandler(
    IApplicationDbContext context,
    IUserContext userContext)
    : IQueryHandler<GetWalletByIdQuery, WalletResponse>
{
    private const int RecentTransactionCount = 50;

    public async Task<Result<WalletResponse>> Handle(
        GetWalletByIdQuery query,
        CancellationToken cancellationToken)
    {
        if (query.UserId != userContext.UserId)
        {
            return Result.Failure<WalletResponse>(WalletErrors.Unauthorized);
        }

        Wallet? wallet = await context.Wallets
            .Include(w => w.Balances)
            .AsNoTracking()
            .SingleOrDefaultAsync(w => w.Id == query.WalletId, cancellationToken);

        if (wallet is null)
        {
            return Result.Failure<WalletResponse>(WalletErrors.NotFound(query.WalletId));
        }

        if (wallet.UserId != query.UserId)
        {
            return Result.Failure<WalletResponse>(WalletErrors.Unauthorized);
        }

        List<WalletTransaction> recentTransactions = await context.WalletTransactions
            .AsNoTracking()
            .Where(t => t.WalletId == query.WalletId)
            .OrderByDescending(t => t.OccurredAt)
            .Take(RecentTransactionCount)
            .ToListAsync(cancellationToken);

        WalletResponse response = new()
        {
            Id = wallet.Id,
            Name = wallet.Name,
            CreatedAt = wallet.CreatedAt,
            Balances = wallet.Balances
                .Select(b => new BalanceResponse
                {
                    CurrencyCode = b.CurrencyCode.Value,
                    Amount = b.Amount
                })
                .ToList(),
            RecentTransactions = recentTransactions
                .Select(t => new TransactionResponse
                {
                    Id = t.Id,
                    Type = t.Type.ToString(),
                    CurrencyCode = t.CurrencyCode.Value,
                    Amount = t.Amount,
                    OccurredAt = t.OccurredAt
                })
                .ToList()
        };

        return response;
    }
}

