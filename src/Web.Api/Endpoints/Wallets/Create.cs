using Application.Abstractions.Authentication;
using Application.Abstractions.Messaging;
using Application.Wallets.Commands.CreateWallet;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Wallets;

internal sealed class Create : IEndpoint
{
    public sealed record Request(string Name);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("wallets", async (
            Request request,
            ICommandHandler<CreateWalletCommand, Guid> handler,
            IUserContext userContext,
            CancellationToken cancellationToken) =>
        {
            var command = new CreateWalletCommand(userContext.UserId, request.Name);

            Result<Guid> result = await handler.Handle(command, cancellationToken);

            return result.Match(
                id => Results.Created($"/wallets/{id}", new { id }),
                CustomResults.Problem);
        })
        .WithTags(Tags.Wallets)
        .RequireAuthorization();
    }
}

