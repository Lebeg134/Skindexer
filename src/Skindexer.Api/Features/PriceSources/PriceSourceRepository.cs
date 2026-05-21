using Microsoft.EntityFrameworkCore;
using Skindexer.Contracts.Constants;
using Skindexer.Contracts.Models;
using Skindexer.Contracts.Responses;
using Skindexer.Api.Data;

namespace Skindexer.Api.Features.PriceSources;

public class PriceSourceRepository(SkindexerDbContext db) : IPriceSourceRepository
{
    public async Task<IReadOnlyList<PriceSourceResponse>> GetPriceSourcesAsync(
        string gameId,
        PriceSourceQueryParams queryParams,
        CancellationToken ct = default)
    {
        var query = db.CurrentPrices
            .Where(p => p.GameId == gameId);

        if (!string.IsNullOrWhiteSpace(queryParams.ItemType))
        {
            query = query.Where(p =>
                db.Variants
                    .Where(v => v.Id == p.VariantId)
                    .Any(v => v.Item.ItemType == queryParams.ItemType));
        }

        var rows = await query
            .Select(p => new { p.Source, p.PriceType })
            .Distinct()
            .ToListAsync(ct);

        return rows
            .GroupBy(r => r.Source)
            .Select(g => new PriceSourceResponse(
                Id: g.Key,
                Name: Sources.GetDisplayName(g.Key),
                PriceTypes: g.Select(r => r.PriceType).ToList()
            ))
            .OrderBy(r => r.Id)
            .ToList();
    }
}