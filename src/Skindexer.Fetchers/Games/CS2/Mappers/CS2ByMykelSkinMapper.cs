using Microsoft.Extensions.Logging;
using Skindexer.Contracts.Constants;
using Skindexer.Contracts.Models;
using Skindexer.Contracts.Models.Metadata;
using Skindexer.Fetchers.Games.CS2.Fetchers.DTOs;
using Skindexer.Fetchers.Games.CS2.SlugHelpers;

namespace Skindexer.Fetchers.Games.CS2.Mappers;

public class CS2ByMykelSkinMapper(ILogger<CS2ByMykelSkinMapper> logger)
    : CS2ByMykelMapperBase<ByMykelSkin>(logger, "skins.json", CS2ItemTypes.WeaponSkin)
{
    protected override string? GetName(ByMykelSkin dto) => dto.Name;
    protected override string? GetDiscriminator(ByMykelSkin dto) => dto.PaintIndex;

    protected override SkinItem? MapItem(ByMykelSkin dto, string slug)
    {
        if (dto.Name is null) return null;

        var wears = dto.Wears?.Select(w => w.Name ?? "").Where(w => w != "").ToList() ?? [];
        var first = dto.Collections.FirstOrDefault();

        var metadata = new CS2WeaponSkinMetadata
        {
            WeaponId = dto.Weapon?.Id,
            WeaponName = dto.Weapon?.Name,
            Rarity = dto.Rarity?.Name,
            RarityColor = dto.Rarity?.Color,
            Category = dto.Category?.Name,
            Collection = first?.Name,
            AvailableWears = wears,
            MinFloat = dto.MinFloat,
            MaxFloat = dto.MaxFloat,
            PaintIndex = dto.PaintIndex,
            PatternName = dto.Pattern?.Name,
            Description = dto.Description,
        };

        return new SkinItem
        {
            Id = Guid.NewGuid(),
            GameId = GameIds.CounterStrike,
            ItemType = ItemType,
            Slug = slug,
            Name = dto.Name,
            ImageUrl = dto.Image,
            IsTradeable = true,
            IsMarketable = true,
            Metadata = metadata.ToDictionary(),
        };
    }

    protected override IReadOnlyList<SkinVariant> MapVariants(ByMykelSkin dto, SkinItem item)
    {
        var wears = dto.Wears?.Select(w => w.Name ?? "").Where(w => w != "").ToList() ?? [];
        if (wears.Count == 0) return [];

        var variants = new List<SkinVariant>();

        var editions = new List<(bool StatTrak, bool Souvenir)> { (false, false) };
        if (dto.Stattrak) editions.Add((true, false));
        if (dto.Souvenir) editions.Add((false, true));

        foreach (var wear in wears)
        {
            foreach (var (statTrak, souvenir) in editions)
            {
                var variantSlug = CS2ByMykelSlugHelper.BuildVariantSlug(item.Slug, wear, statTrak, souvenir);

                variants.Add(new SkinVariant
                {
                    Id = Guid.NewGuid(),
                    ItemId = item.Id,
                    GameId = item.GameId,
                    Slug = variantSlug,
                    Metadata = new Dictionary<string, object?>
                    {
                        ["Wear"] = wear,
                        ["StatTrak"] = statTrak,
                        ["Souvenir"] = souvenir,
                    },
                });
            }
        }

        return variants;
    }
}