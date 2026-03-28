using Microsoft.Extensions.Logging;
using Skindexer.Contracts.Models;
using Skindexer.Fetchers.Games.CS2.Fetchers.DTOs;
using Skindexer.Fetchers.Games.CS2.Metadata;

namespace Skindexer.Fetchers.Games.CS2.Mappers;

public class CS2ByMykelPatchMapper(ILogger<CS2ByMykelPatchMapper> logger)
    : CS2ByMykelMapperBase<ByMykelPatch>(logger, "patches.json")
{
    protected override string? GetName(ByMykelPatch dto) => dto.Name;
    protected override string? GetDiscriminator(ByMykelPatch dto) => dto.DefIndex;

    protected override SkinItem? MapItem(ByMykelPatch dto, string slug)
    {
        if (string.IsNullOrWhiteSpace(dto.Id) || string.IsNullOrWhiteSpace(dto.Name))
            return null;

        var metadata = new CS2PatchMetadata
        {
            Rarity = dto.Rarity?.Name,
            RarityColor = dto.Rarity?.Color,
        };

        return new SkinItem
        {
            Id = Guid.NewGuid(),
            GameId = "cs2",
            Slug = slug,
            Name = dto.Name,
            ImageUrl = dto.Image,
            IsTradeable = true,
            IsMarketable = dto.MarketHashName is not null,
            Metadata = metadata.ToDictionary(),
        };
    }
}