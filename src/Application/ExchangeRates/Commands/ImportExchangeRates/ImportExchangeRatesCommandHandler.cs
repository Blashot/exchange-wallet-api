using Application.Abstractions.Data;
using Application.Abstractions.ExchangeRates;
using Application.Abstractions.Messaging;
using Domain.ExchangeRates;
using Domain.Shared;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.ExchangeRates.Commands.ImportExchangeRates;

internal sealed class ImportExchangeRatesCommandHandler(
    INbpClient nbpClient,
    IApplicationDbContext context,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<ImportExchangeRatesCommand, ImportExchangeRatesResult>
{
    public async Task<Result<ImportExchangeRatesResult>> Handle(
        ImportExchangeRatesCommand command,
        CancellationToken cancellationToken)
    {
        Result<IReadOnlyList<NbpTableData>> fetchResult =
            await nbpClient.GetTableBRatesAsync(cancellationToken);

        if (fetchResult.IsFailure)
        {
            return Result.Failure<ImportExchangeRatesResult>(fetchResult.Error);
        }

        IReadOnlyList<NbpTableData> tables = fetchResult.Value;


        if (tables.Count == 0)
        {
            return Result.Success(new ImportExchangeRatesResult(0, 0));
        }
        
        
        var alreadyImported = (await context.ExchangeRateTables
                .AsNoTracking()
                .Where(t => tables.Select(d => d.TableNumber).Contains(t.TableNumber))
                .Select(t => t.TableNumber)
                .ToListAsync(cancellationToken))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        int tablesImported = 0;
        int ratesImported = 0;
        DateTime importedAt = dateTimeProvider.UtcNow;

        //Import only new tables; skip duplicates (idempotent).
        foreach (NbpTableData tableData in tables)
        {
            if (alreadyImported.Contains(tableData.TableNumber))
            {
                continue;
            }

            IEnumerable<(CurrencyCode, string, decimal)> rates = tableData.Rates
                .Select(r => (new CurrencyCode(r.CurrencyCode), r.CurrencyName, r.MidRate));

            var table = ExchangeRateTable.Create(
                tableData.TableNumber,
                tableData.EffectiveDate,
                importedAt,
                rates);

            context.ExchangeRateTables.Add(table);

            tablesImported++;
            ratesImported += tableData.Rates.Count;
        }


        if (tablesImported > 0)
        {
            await context.SaveChangesAsync(cancellationToken);
        }

        return Result.Success(new ImportExchangeRatesResult(tablesImported, ratesImported));
    }
}




