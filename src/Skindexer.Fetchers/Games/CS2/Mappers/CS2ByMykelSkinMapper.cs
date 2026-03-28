using Microsoft.Extensions.Logging;
using Skindexer.Contracts.Models;
using Skindexer.Fetchers.Games.CS2.Fetchers.DTOs;
using Skindexer.Fetchers.Games.CS2.Metadata;

namespace Skindexer.Fetchers.Games.CS2.Mappers;

public class CS2ByMykelSkinMapper(ILogger<CS2ByMykelSkinMapper> logger)
    : CS2ByMykelMapperBase<ByMykelSkin>(logger, "skins.json")
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
            StatTrak = dto.Stattrak,
            Souvenir = dto.Souvenir,
            MinFloat = dto.MinFloat,
            MaxFloat = dto.MaxFloat,
            PaintIndex = dto.PaintIndex,
            PatternName = dto.Pattern?.Name,
            Description = dto.Description,
        };

        return new SkinItem
        {
            Id = Guid.NewGuid(),
            GameId = "cs2",
            Slug = slug,
            Name = dto.Name,
            ImageUrl = dto.Image,
            IsTradeable = true,
            IsMarketable = true,
            Metadata = metadata.ToDictionary(),
        };
    }
}