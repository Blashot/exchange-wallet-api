using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.ExchangeRates;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.ExchangeRates.Queries.GetLatestExchangeRates;

internal sealed class GetLatestExchangeRatesQueryHandler(IApplicationDbContext context)
    : IQueryHandler<GetLatestExchangeRatesQuery, ExchangeRatesResponse>
{
    public async Task<Result<ExchangeRatesResponse>> Handle(
        GetLatestExchangeRatesQuery query,
        CancellationToken cancellationToken)
    {
        ExchangeRateTable? table = await context.ExchangeRateTables
            .Include(t => t.Rates)
            .AsNoTracking()
            .OrderByDescending(t => t.PublicationDate)
            .FirstOrDefaultAsync(cancellationToken);

        if (table is null)
        {
            return Result.Failure<ExchangeRatesResponse>(ExchangeRateErrors.NoRatesAvailable);
        }

        ExchangeRatesResponse response = new()
        {
            Id = table.Id,
            TableNumber = table.TableNumber,
            PublicationDate = table.PublicationDate,
            ImportedAt = table.ImportedAt,
            Rates = table.Rates
                .Select(r => new ExchangeRateItemResponse
                {
                    CurrencyCode = r.CurrencyCode.Value,
                    CurrencyName = r.CurrencyName,
                    MidRate = r.MidRate
                })
                .OrderBy(r => r.CurrencyCode)
                .ToList()
        };

        return response;
    }
}

