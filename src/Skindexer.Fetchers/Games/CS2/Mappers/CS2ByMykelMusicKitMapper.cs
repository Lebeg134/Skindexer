using Microsoft.Extensions.Logging;
using Skindexer.Contracts.Constants;
using Skindexer.Contracts.Models;
using Skindexer.Contracts.Models.Metadata;
using Skindexer.Fetchers.Games.CS2.Fetchers.DTOs;
using Skindexer.Fetchers.Games.CS2.Mappers;

namespace Skindexer.Fetchers.Games.CS2.Mappers;

public class CS2ByMykelMusicKitMapper(ILogger<CS2ByMykelMusicKitMapper> logger)
    : CS2ByMykelMapperBase<ByMykelMusicKit>(logger, "music_kits.json", CS2ItemTypes.MusicKit)
{
    protected override string? GetName(ByMykelMusicKit dto) => dto.Name;
    protected override string? GetDiscriminator(ByMykelMusicKit dto) => dto.DefIndex;

    protected override SkinItem? MapItem(ByMykelMusicKit dto, string slug)
    {
        if (string.IsNullOrWhiteSpace(dto.Id) || string.IsNullOrWhiteSpace(dto.Name))
            return null;

        var metadata = new CS2MusicKitMetadata
        {
            Rarity = dto.Rarity?.Name,
            RarityColor = dto.Rarity?.Color,
            Exclusive = dto.Exclusive,
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
            IsMarketable = dto.MarketHashName is not null,
            Metadata = metadata.ToDictionary(),
        };
    }
}