using Skindexer.Contracts.Models;

namespace Skindexer.Api.Features.Items;

public interface IItemRepository
{
    Task<IReadOnlyList<SkinItem>> GetItemsByGameAsync(string gameId, CancellationToken ct = default);
    Task UpsertItemsAsync(IReadOnlyList<SkinItem> items, CancellationToken ct = default);
}