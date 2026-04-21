namespace Skindexer.Api.Features.Rarity;

public static class GetRarityGroupsEndpoint
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/games/{gameId}/rarity-groups", async (
            string gameId,
            IRarityGroupRepository repository,
            CancellationToken ct) =>
        {
            var groups = await repository.GetRarityGroupsByGameAsync(gameId, ct);
            return Results.Ok(groups);
        });
    }
}