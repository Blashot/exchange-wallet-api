using Application.Abstractions.Messaging;
using Application.ExchangeRates.Queries.GetLatestExchangeRates;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.ExchangeRates;

internal sealed class GetLatest : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("exchange-rates/latest", async (
            IQueryHandler<GetLatestExchangeRatesQuery, ExchangeRatesResponse> handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetLatestExchangeRatesQuery();

            Result<ExchangeRatesResponse> result = await handler.Handle(query, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.ExchangeRates)
        .RequireAuthorization();
    }
}

