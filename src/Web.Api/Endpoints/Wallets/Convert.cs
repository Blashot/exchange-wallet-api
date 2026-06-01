using Application.Abstractions.Authentication;
using Application.Abstractions.Messaging;
using Application.Wallets.Commands.ConvertCurrency;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Wallets;

internal sealed class Convert : IEndpoint
{
    public sealed record Request(string FromCurrencyCode, string ToCurrencyCode, decimal Amount);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("wallets/{id:guid}/convert", async (
            Guid id,
            Request request,
            ICommandHandler<ConvertCurrencyCommand> handler,
            IUserContext userContext,
            CancellationToken cancellationToken) =>
        {
            var command = new ConvertCurrencyCommand(
                id,
                userContext.UserId,
                request.FromCurrencyCode,
                request.ToCurrencyCode,
                request.Amount);

            Result result = await handler.Handle(command, cancellationToken);

            return result.Match(Results.NoContent, CustomResults.Problem);
        })
        .WithTags(Tags.Wallets)
        .RequireAuthorization();
    }
}

