using Skindexer.Contracts.Models;

namespace Skindexer.Api.Features.Items;

public interface IItemRepository
{
    Task<IReadOnlyList<SkinItem>> GetItemsByGameAsync(string gameId, ItemQueryParams query, CancellationToken ct = default);
    Task UpsertItemsAsync(IReadOnlyList<SkinItem> items, CancellationToken ct = default);
    Task<IReadOnlyDictionary<string, Guid>> GetSlugToItemIdMapAsync(string gameId, CancellationToken ct = default);
}