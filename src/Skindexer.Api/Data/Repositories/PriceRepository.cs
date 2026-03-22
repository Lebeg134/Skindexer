using Microsoft.EntityFrameworkCore;
using Skindexer.Api.Features.Prices;
using Skindexer.Contracts.Models;

namespace Skindexer.Api.Data.Repositories;

public class PriceRepository : IPriceRepository
{
    private readonly SkindexerDbContext _db;

    public PriceRepository(SkindexerDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<SkinPrice>> GetCurrentPricesByGameAsync(string gameId, CancellationToken ct = default)
    {
        return await _db.Prices
            .Where(p => p.Item.GameId == gameId)
            .GroupBy(p => new { p.ItemId, p.Slug, p.Source, p.PriceType })
            .Select(g => g.OrderByDescending(p => p.RecordedAt).First())
            .Select(p => new SkinPrice
            {
                ItemId = p.ItemId,
                Slug = p.Slug,
                Source = p.Source,
                PriceType = p.PriceType,
                Price = p.Price,
                Currency = p.Currency,
                Volume = p.Volume,
                RecordedAt = p.RecordedAt
            })
            .ToListAsync(ct);
    }
}