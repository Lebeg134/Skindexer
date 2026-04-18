namespace Skindexer.Api.Features.Enrichment;

public static class EnrichEndpoints
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/admin/enrich/{gameId}", async (
            string gameId,
            IServiceScopeFactory scopeFactory,
            ILogger<Program> logger,
            CancellationToken ct) =>
        {
            logger.LogInformation("Enrichment triggered for {GameId}", gameId);

            _ = Task.Run(async () =>
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var enrichers = scope.ServiceProvider
                    .GetServices<IItemEnricher>()
                    .Where(e => e.GameId == gameId)
                    .ToList();

                if (enrichers.Count == 0)
                {
                    logger.LogWarning(
                        "No enricher registered for {GameId} — nothing to do",
                        gameId);
                    return;
                }

                foreach (var enricher in enrichers)
                {
                    try
                    {
                        await enricher.EnrichAsync();
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex,
                            "Enricher {EnricherType} failed for {GameId}",
                            enricher.GetType().Name, gameId);
                    }
                }

                logger.LogInformation(
                    "Enrichment complete for {GameId} ({Count} enricher(s) ran)",
                    gameId, enrichers.Count);

            }, CancellationToken.None);

            return Results.Accepted();
        });
    }
}