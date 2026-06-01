using Application.Abstractions.Authentication;
using Application.Abstractions.Messaging;
using Application.Wallets.Queries.GetWalletById;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Wallets;

internal sealed class GetById : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("wallets/{id:guid}", async (
            Guid id,
            IQueryHandler<GetWalletByIdQuery, WalletResponse> handler,
            IUserContext userContext,
            CancellationToken cancellationToken) =>
        {
            var query = new GetWalletByIdQuery(id, userContext.UserId);

            Result<WalletResponse> result = await handler.Handle(query, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Wallets)
        .RequireAuthorization();
    }
}

