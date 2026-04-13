using Skindexer.Api.Features.Variants;
using Skindexer.Fetchers.Games.CS2.Fetchers;
using Skindexer.Fetchers.Interfaces;

namespace Skindexer.Api.Features.Import;

public static class CS2KaggleImportEndpoints
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/admin/import/cs2/kaggle", async (
            IServiceScopeFactory scopeFactory,
            ILogger<Program> logger,
            CancellationToken ct) =>
        {
            logger.LogInformation("CS2 Kaggle import triggered");

            _ = Task.Run(async () =>
            {
                await using var scope = scopeFactory.CreateAsyncScope();

                var fetcher = scope.ServiceProvider.GetRequiredService<CS2KagglePriceFetcher>();
                var persister = scope.ServiceProvider.GetRequiredService<IFetchResultPersister>();
                var variantRepo = scope.ServiceProvider.GetRequiredService<IVariantRepository>();

                var slugMap = await variantRepo.GetSlugToVariantIdMapAsync("cs2", CancellationToken.None);
                var result = await fetcher.FetchAsync(new CS2KaggleFetchContext { VariantSlugMap = slugMap }, CancellationToken.None);


                 if (!result.IsSuccess)
                {
                    logger.LogError(
                        "CS2 Kaggle fetch failed: {Error}",
                        result.ErrorMessage);
                    return;
                }

                await persister.PersistAsync(result);
            }, CancellationToken.None);

            return Results.Accepted();
        });
    }
}