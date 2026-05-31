using Application.Abstractions.Messaging;
using Application.ExchangeRates.Commands.ImportExchangeRates;
using Microsoft.Extensions.Logging;
using SharedKernel;

namespace Infrastructure.BackgroundJobs;


internal sealed class ImportExchangeRatesJob(
    ICommandHandler<ImportExchangeRatesCommand, ImportExchangeRatesResult> handler,
    ILogger<ImportExchangeRatesJob> logger)
{

    public async Task ExecuteAsync()
    {
        Result<ImportExchangeRatesResult> result =
            await handler.Handle(new ImportExchangeRatesCommand(), CancellationToken.None);

        if (result.IsFailure)
        {
            logger.LogError(
                "Exchange rate import failed: [{ErrorCode}] {ErrorDescription}",
                result.Error.Code,
                result.Error.Description);

            return;
        }

        if (logger.IsEnabled(LogLevel.Information))
        {
            if (result.Value.TablesImported == 0)
            {
                logger.LogInformation(
                    "NBP exchange rate import: no new tables available");
            }
            else
            {
                logger.LogInformation(
                    "NBP exchange rate import completed: {TablesImported} table(s), {RatesImported} rate(s)",
                    result.Value.TablesImported,
                    result.Value.RatesImported);
            }
        }
    }
}

