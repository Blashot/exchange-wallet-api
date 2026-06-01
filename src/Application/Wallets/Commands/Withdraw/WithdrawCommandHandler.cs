using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Shared;
using Domain.Wallets;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Wallets.Commands.Withdraw;

internal sealed class WithdrawCommandHandler(
    IApplicationDbContext context,
    IUserContext userContext,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<WithdrawCommand>
{
    public async Task<Result> Handle(WithdrawCommand command, CancellationToken cancellationToken)
    {
        if (command.UserId != userContext.UserId)
        {
            return Result.Failure(WalletErrors.Unauthorized);
        }

        Wallet? wallet = await context.Wallets
            .Include(w => w.Balances)
            .SingleOrDefaultAsync(w => w.Id == command.WalletId, cancellationToken);

        if (wallet is null)
        {
            return Result.Failure(WalletErrors.NotFound(command.WalletId));
        }

        if (wallet.UserId != command.UserId)
        {
            return Result.Failure(WalletErrors.Unauthorized);
        }

        Result withdrawResult = wallet.Withdraw(
            new CurrencyCode(command.CurrencyCode),
            command.Amount,
            dateTimeProvider.UtcNow);

        if (withdrawResult.IsFailure)
        {
            return withdrawResult;
        }

        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result.Failure(WalletErrors.ConcurrencyConflict);
        }

        return Result.Success();
    }
}

