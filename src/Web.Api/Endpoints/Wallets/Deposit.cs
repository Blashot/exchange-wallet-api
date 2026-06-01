using Application.Abstractions.Authentication;
using Application.Abstractions.Messaging;
using Application.Wallets.Commands.Deposit;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Wallets;

internal sealed class Deposit : IEndpoint
{
    public sealed record Request(string CurrencyCode, decimal Amount);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("wallets/{id:guid}/deposit", async (
            Guid id,
            Request request,
            ICommandHandler<DepositCommand> handler,
            IUserContext userContext,
            CancellationToken cancellationToken) =>
        {
            var command = new DepositCommand(id, userContext.UserId, request.CurrencyCode, request.Amount);

            Result result = await handler.Handle(command, cancellationToken);

            return result.Match(Results.NoContent, CustomResults.Problem);
        })
        .WithTags(Tags.Wallets)
        .RequireAuthorization();
    }
}

