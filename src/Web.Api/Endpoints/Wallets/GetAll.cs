using Application.Abstractions.Authentication;
using Application.Abstractions.Messaging;
using Application.Wallets.Queries.GetWallets;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Wallets;

internal sealed class GetAll : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("wallets", async (
            IQueryHandler<GetWalletsQuery, List<WalletSummaryResponse>> handler,
            IUserContext userContext,
            CancellationToken cancellationToken) =>
        {
            var query = new GetWalletsQuery(userContext.UserId);

            Result<List<WalletSummaryResponse>> result = await handler.Handle(query, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Wallets)
        .RequireAuthorization();
    }
}

