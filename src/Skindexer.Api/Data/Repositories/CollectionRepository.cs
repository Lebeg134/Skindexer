using Microsoft.EntityFrameworkCore;
using Skindexer.Api.Features.Collections;
using Skindexer.Contracts.Responses;

namespace Skindexer.Api.Data.Repositories;

public class CollectionRepository : ICollectionRepository
{
    private readonly SkindexerDbContext _db;

    public CollectionRepository(SkindexerDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<CollectionResponse>> GetCollectionsByGameAsync(string gameId, CancellationToken ct = default)
    {
        return await _db.Collections
            .Where(c => c.GameId == gameId)
            .Select(c => new CollectionResponse
            {
                Slug = c.Slug,
                DisplayName = c.Name,
                ItemIds = c.Items.Select(i => i.Id).ToList()
            })
            .ToListAsync(ct);
    }
}