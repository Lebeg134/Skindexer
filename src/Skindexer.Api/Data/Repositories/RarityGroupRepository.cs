using Microsoft.EntityFrameworkCore;
using Skindexer.Api.Features.Rarity;
using Skindexer.Contracts.Responses;

namespace Skindexer.Api.Data.Repositories;

public class RarityGroupRepository : IRarityGroupRepository
{
    private readonly SkindexerDbContext _db;

    public RarityGroupRepository(SkindexerDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<RarityGroupResponse>> GetRarityGroupsByGameAsync(string gameId, CancellationToken ct = default)
    {
        return await _db.RarityGroups
            .Where(rg => rg.GameId == gameId)
            .Select(rg => new RarityGroupResponse
            {
                Slug = rg.Slug,
                DisplayName = rg.Name
            })
            .ToListAsync(ct);
    }
}