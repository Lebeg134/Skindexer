namespace Skindexer.Api.Features.Rarity;

public static class GetRaritiesEndpoint
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/games/{gameId}/rarities", async (string gameId, IRarityRepository repository, CancellationToken ct) =>
        {
            var rarities = await repository.GetRaritiesByGameAsync(gameId, ct);
            return Results.Ok(rarities);
        });
    }
}