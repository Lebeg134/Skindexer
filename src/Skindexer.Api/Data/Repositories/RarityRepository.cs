using Microsoft.EntityFrameworkCore;
using Skindexer.Api.Features.Rarity;
using Skindexer.Contracts.Responses;

namespace Skindexer.Api.Data.Repositories;

public class RarityRepository : IRarityRepository
{
    private readonly SkindexerDbContext _db;

    public RarityRepository(SkindexerDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<RarityResponse>> GetRaritiesByGameAsync(string gameId, CancellationToken ct = default)
    {
        return await _db.Rarities
            .Where(r => r.RarityGroup!.GameId == gameId)
            .OrderBy(r => r.Order)
            .Select(r => new RarityResponse
            {
                Slug = r.Slug,
                DisplayName = r.Name,
                Order = r.Order,
                GroupSlug = r.RarityGroup != null ? r.RarityGroup.Slug : null
            })
            .ToListAsync(ct);
    }
}