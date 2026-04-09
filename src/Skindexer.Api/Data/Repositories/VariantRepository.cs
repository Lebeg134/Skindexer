using Microsoft.EntityFrameworkCore;
using Skindexer.Api.Data.Entities;
using Skindexer.Api.Features.Variants;
using Skindexer.Contracts.Models;

namespace Skindexer.Api.Data.Repositories;

public class VariantRepository : IVariantRepository
{
    private readonly SkindexerDbContext _db;

    public VariantRepository(SkindexerDbContext db)
    {
        _db = db;
    }

    public async Task UpsertVariantsAsync(IReadOnlyList<SkinVariant> variants, CancellationToken ct = default)
    {
        if (variants.Count == 0) return;

        foreach (var batch in variants.Chunk(500))
        {
            var slugs = batch.Select(v => v.Slug).ToList();

            var existing = await _db.Variants
                .Where(v => slugs.Contains(v.Slug))
                .ToDictionaryAsync(v => v.Slug, ct);

            foreach (var variant in batch)
            {
                if (existing.TryGetValue(variant.Slug, out var entity))
                {
                    entity.ItemId   = variant.ItemId;
                    entity.GameId   = variant.GameId;
                    entity.Metadata = variant.Metadata;
                }
                else
                {
                    _db.Variants.Add(new SkinVariantEntity
                    {
                        Id       = variant.Id == Guid.Empty ? Guid.NewGuid() : variant.Id,
                        ItemId   = variant.ItemId,
                        GameId   = variant.GameId,
                        Slug     = variant.Slug,
                        Metadata = variant.Metadata
                    });
                }
            }

            await _db.SaveChangesAsync(ct);
        }
    }

    public async Task<IReadOnlyDictionary<string, Guid>> GetSlugToVariantIdMapAsync(
        string gameId,
        CancellationToken ct = default)
    {
        return await _db.Variants
            .Where(v => v.GameId == gameId)
            .ToDictionaryAsync(v => v.Slug, v => v.Id, ct);
    }
}