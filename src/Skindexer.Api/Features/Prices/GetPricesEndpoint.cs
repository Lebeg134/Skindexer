using Skindexer.Contracts.Responses;

namespace Skindexer.Api.Features.Prices;

public static class GetPricesEndpoint
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/games/{gameId}/prices", async (string gameId, IPriceRepository repository, CancellationToken ct) =>
        {
            var prices = await repository.GetCurrentPricesByGameAsync(gameId, ct);
            return Results.Ok(prices.Select(p => new SkinPriceResponse
            {
                ItemId = p.ItemId,
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