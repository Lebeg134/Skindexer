using Microsoft.EntityFrameworkCore;
using Skindexer.Api.Data;
using Skindexer.Api.Data.Entities;
using Skindexer.Contracts.Constants;

namespace Skindexer.Api.Features.Enrichment;

public class CS2ItemEnricher : IItemEnricher
{
    public string GameId => GameIds.CounterStrike;

    private static readonly HashSet<string> KnifeWeaponIdExceptions = new()
    {
        "weapon_bayonet",
        "weapon_knife_css"
    };

    private readonly SkindexerDbContext _db;

    public CS2ItemEnricher(SkindexerDbContext db)
    {
        _db = db;
    }

    public async Task EnrichAsync(CancellationToken ct = default)
    {
        var items = await _db.Items
            .Where(i => i.GameId == GameId && i.ItemType != string.Empty)
            .ToListAsync(ct);

        var itemsByType = items.GroupBy(i => i.ItemType);

        foreach (var typeGroup in itemsByType)
        {
            var itemType = typeGroup.Key;
            var groupItems = typeGroup.ToList();

            var rarityGroup = await FindOrCreateRarityGroupAsync(itemType, ct);
            await EnrichRaritiesAsync(rarityGroup, groupItems, ct);
            await EnrichCollectionsAsync(itemType, groupItems, ct);
        }

        await _db.SaveChangesAsync(ct);
    }

    // -------------------------------------------------------------------------
    // Metadata extraction
    // -------------------------------------------------------------------------

    private static string? ExtractRarity(string itemType, Dictionary<string, object?> metadata)
    {
        var rarityName = metadata.GetValueOrDefault("Rarity")?.ToString();

        if (itemType == CS2ItemTypes.WeaponSkin && rarityName == "Covert" && IsKnife(metadata))
            return "Extraordinary";

        return rarityName;
    }

    private static string? ExtractCollection(string itemType, Dictionary<string, object?> metadata)
    {
        var hasCollection = itemType is
            CS2ItemTypes.WeaponSkin or
            CS2ItemTypes.Agent or
            CS2ItemTypes.Keychain or
            CS2ItemTypes.Sticker;

        return hasCollection
            ? metadata.GetValueOrDefault("Collection")?.ToString()
            : null;
    }

    private static bool IsKnife(Dictionary<string, object?> metadata)
    {
        var weaponId = metadata.GetValueOrDefault("WeaponId")?.ToString();
        if (string.IsNullOrEmpty(weaponId)) return false;

        return weaponId.StartsWith("weapon_knife_", StringComparison.OrdinalIgnoreCase)
            || KnifeWeaponIdExceptions.Contains(weaponId);
    }

    // -------------------------------------------------------------------------
    // Rarity group
    // -------------------------------------------------------------------------

    private async Task<RarityGroupEntity> FindOrCreateRarityGroupAsync(string itemType, CancellationToken ct)
    {
        var group = await _db.RarityGroups
            .FirstOrDefaultAsync(g => g.GameId == GameId && g.Type == itemType, ct);

        if (group is not null)
            return group;

        group = new RarityGroupEntity
        {
            Id = Guid.NewGuid(),
            GameId = GameId,
            Type = itemType,
            Slug = itemType,
            Name = itemType
        };

        _db.RarityGroups.Add(group);
        await _db.SaveChangesAsync(ct);

        return group;
    }

    // -------------------------------------------------------------------------
    // Rarities
    // -------------------------------------------------------------------------

    private async Task EnrichRaritiesAsync(
        RarityGroupEntity rarityGroup,
        List<SkinItemEntity> items,
        CancellationToken ct)
    {
        var existingRarities = await _db.Rarities
            .Where(r => r.RarityGroupId == rarityGroup.Id)
            .ToListAsync(ct);

        var rarityMap = existingRarities.ToDictionary(r => r.Slug);

        foreach (var item in items)
        {
            var rarityName = ExtractRarity(rarityGroup.Type, item.Metadata);
            if (string.IsNullOrWhiteSpace(rarityName)) continue;

            var slug = Slugify(rarityName);

            if (!rarityMap.TryGetValue(slug, out var rarity))
            {
                rarity = new RarityEntity
                {
                    Id = Guid.NewGuid(),
                    RarityGroupId = rarityGroup.Id,
                    Slug = slug,
                    Name = rarityName,
                    Order = null
                };

                _db.Rarities.Add(rarity);
                rarityMap[slug] = rarity;
            }

            item.RarityId = rarity.Id;
        }
    }

    // -------------------------------------------------------------------------
    // Collections
    // -------------------------------------------------------------------------

    private async Task EnrichCollectionsAsync(
        string itemType,
        List<SkinItemEntity> items,
        CancellationToken ct)
    {
        var existingCollections = await _db.Collections
            .Where(c => c.GameId == GameId && c.Type == itemType)
            .ToListAsync(ct);

        var collectionMap = existingCollections.ToDictionary(c => c.Slug);

        foreach (var item in items)
        {
            var collectionName = ExtractCollection(itemType, item.Metadata);
            if (string.IsNullOrWhiteSpace(collectionName)) continue;

            var slug = Slugify(collectionName);

            if (!collectionMap.TryGetValue(slug, out var collection))
            {
                collection = new CollectionEntity
                {
                    Id = Guid.NewGuid(),
                    GameId = GameId,
                    Type = itemType,
                    Slug = slug,
                    Name = collectionName
                };

                _db.Collections.Add(collection);
                collectionMap[slug] = collection;
            }

            item.CollectionId = collection.Id;
        }
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static string Slugify(string value)
        => value.ToLowerInvariant().Replace(" ", "_");
}