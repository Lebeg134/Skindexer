using Skindexer.Contracts.Models;

using Skindexer.Contracts.Responses;

namespace Skindexer.Api.Features.Variants;

public static class GetVariantsEndpoint
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/games/{gameId}/variants", async (
            string gameId,
            [AsParameters] VariantQueryParams query,
            IVariantRepository repository,
            CancellationToken ct) =>
        {
            var variants = await repository.GetVariantsByGameAsync(gameId, query, ct);
            return Results.Ok(variants.Select(v => new SkinVariantResponse
            {
                Id = v.Id,
                ItemId = v.ItemId,
                Slug = v.Slug,
                Metadata = v.Metadata
            }));
        });
    }
}