using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.ExchangeRates;
using Domain.Shared;
using Domain.Wallets;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Wallets.Commands.ConvertCurrency;

internal sealed class ConvertCurrencyCommandHandler(
    IApplicationDbContext context,
    IUserContext userContext,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<ConvertCurrencyCommand>
{
    public async Task<Result> Handle(ConvertCurrencyCommand command, CancellationToken cancellationToken)
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
        
        // Conversion goes through PLN as pivot: fromCurrency → PLN → toCurrency.
        ExchangeRateTable? latestTable = await context.ExchangeRateTables
            .Include(t => t.Rates)
            .AsNoTracking()
            .OrderByDescending(t => t.PublicationDate)
            .FirstOrDefaultAsync(cancellationToken);

        if (latestTable is null)
        {
            return Result.Failure(ExchangeRateErrors.NoRatesAvailable);
        }

        var fromCurrency = new CurrencyCode(command.FromCurrencyCode);
        var toCurrency = new CurrencyCode(command.ToCurrencyCode);

        Result<decimal> fromRateResult = latestTable.GetMidRate(fromCurrency);
        if (fromRateResult.IsFailure)
        {
            return Result.Failure(fromRateResult.Error);
        }

        Result<decimal> toRateResult = latestTable.GetMidRate(toCurrency);
        if (toRateResult.IsFailure)
        {
            return Result.Failure(toRateResult.Error);
        }

        Result convertResult = wallet.Convert(
            fromCurrency,
            toCurrency,
            command.Amount,
            fromRateResult.Value,
            toRateResult.Value,
            dateTimeProvider.UtcNow);

        if (convertResult.IsFailure)
        {
            return convertResult;
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

