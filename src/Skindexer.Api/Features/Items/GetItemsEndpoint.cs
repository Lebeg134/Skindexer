using Skindexer.Contracts.Models;
using Skindexer.Contracts.Responses;

namespace Skindexer.Api.Features.Items;

public static class GetItemsEndpoint
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/games/{gameId}/items", async (
            string gameId,
            [AsParameters] ItemQueryParams query,
            IItemRepository repository,
            CancellationToken ct) =>
        {
            var items = await repository.GetItemsByGameAsync(gameId, query, ct);
            return Results.Ok(items.Select(i => new SkinItemResponse
            {
                Id = i.Id,
                ItemType = i.ItemType,
                Slug = i.Slug,
                Name = i.Name,
                ImageUrl = i.ImageUrl,
                IsTradeable = i.IsTradeable,
                IsMarketable = i.IsMarketable,
                Metadata = i.Metadata,
                AddedToGameAt = i.AddedToGameAt,
                CollectionId = i.CollectionId,
                RarityId = i.RarityId
            }));
        });
    }
}