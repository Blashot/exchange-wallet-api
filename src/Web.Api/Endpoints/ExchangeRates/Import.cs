using Application.Abstractions.Messaging;
using Application.ExchangeRates.Commands.ImportExchangeRates;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.ExchangeRates;

internal sealed class Import : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("exchange-rates/import", async (
            ICommandHandler<ImportExchangeRatesCommand, ImportExchangeRatesResult> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new ImportExchangeRatesCommand();

            Result<ImportExchangeRatesResult> result = await handler.Handle(command, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.ExchangeRates)
        //TODO: Add role-based authorization once the permission system is implemented.
        .RequireAuthorization();
    }
}

