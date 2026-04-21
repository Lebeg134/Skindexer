using Skindexer.Contracts.Models;
using Skindexer.Contracts.Responses;

namespace Skindexer.Api.Features.Prices;

public static class GetPricesEndpoint
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/games/{gameId}/prices", async (
            string gameId,
            [AsParameters] PriceQueryParams query,
            IPriceRepository repository,
            CancellationToken ct) =>
        {
            var prices = await repository.GetCurrentPricesByGameAsync(gameId, query, ct);
            return Results.Ok(prices.Select(p => new SkinPriceResponse
            {
                VariantId = p.VariantId,
                Slug = p.Slug,
                Source = p.Source,
                PriceType = p.PriceType,
                Price = p.Price,
                Currency = p.Currency,
                Volume = p.Volume,
                RecordedAt = p.RecordedAt
            }));
        });
    }
}