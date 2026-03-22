namespace Skindexer.Api.Features.Collections;

public static class GetCollectionsEndpoint
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/games/{gameId}/collections", async (string gameId, ICollectionRepository repository, CancellationToken ct) =>
        {
            var collections = await repository.GetCollectionsByGameAsync(gameId, ct);
            return Results.Ok(collections);
        });
    }
}