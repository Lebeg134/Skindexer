using Skindexer.Contracts.Models;
using Microsoft.AspNetCore.Mvc;

namespace Skindexer.Api.Features.PriceSources;

public static class GetPriceSourcesEndpoint
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/games/{gameId}/price-sources", async (
                string gameId,
                [AsParameters] PriceSourceQueryParams queryParams,
                IPriceSourceRepository repository,
                CancellationToken ct) =>
            {
                var result = await repository.GetPriceSourcesAsync(gameId, queryParams, ct);
                return Results.Ok(result);
            })
            .WithName("GetPriceSources")
            .WithTags("Prices");
    }
}