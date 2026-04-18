using Microsoft.Extensions.Logging;
using Skindexer.Contracts.Constants;
using Skindexer.Contracts.Models;
using Skindexer.Contracts.Models.Metadata;
using Skindexer.Fetchers.Games.CS2.Fetchers.DTOs;

namespace Skindexer.Fetchers.Games.CS2.Mappers;

public class CS2ByMykelCollectibleMapper(ILogger<CS2ByMykelCollectibleMapper> logger)
    : CS2ByMykelMapperBase<ByMykelCollectible>(logger, "collectibles.json", CS2ItemTypes.Collectible)
{
    protected override string? GetName(ByMykelCollectible dto) => dto.Name;
    protected override string? GetDiscriminator(ByMykelCollectible dto) => dto.DefIndex;

    protected override SkinItem? MapItem(ByMykelCollectible dto, string slug)
    {
        if (string.IsNullOrWhiteSpace(dto.Id) || string.IsNullOrWhiteSpace(dto.Name))
            return null;

        var metadata = new CS2CollectibleMetadata
        {
            Rarity = dto.Rarity?.Name,
            RarityColor = dto.Rarity?.Color,
            Type = dto.Type,
            Genuine = dto.Genuine,
            DefIndex = dto.DefIndex,
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