using Microsoft.EntityFrameworkCore;
using Skindexer.Api.Features.Items;
using Skindexer.Contracts.Models;

namespace Skindexer.Api.Data.Repositories;

public class ItemRepository : IItemRepository
{
    private readonly SkindexerDbContext _db;

    public ItemRepository(SkindexerDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<SkinItem>> GetItemsByGameAsync(string gameId, CancellationToken ct = default)
    {
        return await _db.Items
            .Where(i => i.GameId == gameId)
            .Select(i => new SkinItem
            {
                Id = i.Id,
                GameId = i.GameId,
                Slug = i.Slug,
                Name = i.Name,
                ImageUrl = i.ImageUrl,
                IsTradeable = i.IsTradeable,
                IsMarketable = i.IsMarketable,
                Metadata = i.Metadata,
                AddedToGameAt = i.AddedToGameAt,
                CreatedAt = i.CreatedAt,
                UpdatedAt = i.UpdatedAt
            })
            .ToListAsync(ct);
    }
}
