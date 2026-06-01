using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Shared;
using Domain.Wallets;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Wallets.Commands.Deposit;

internal sealed class DepositCommandHandler(
    IApplicationDbContext context,
    IUserContext userContext,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<DepositCommand>
{
    public async Task<Result> Handle(DepositCommand command, CancellationToken cancellationToken)
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

        Result depositResult = wallet.Deposit(
            new CurrencyCode(command.CurrencyCode),
            command.Amount,
            dateTimeProvider.UtcNow);

        if (depositResult.IsFailure)
        {
            return depositResult;
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

