using System.Text.Json.Serialization;

namespace Skindexer.Fetchers.Games.CS2.Fetchers.ByMykelItemFetcher.DTOs;

public record ByMykelSkin(
    string? Id,
    string? Name,
    string? Description,
    ByMykelWeapon? Weapon,
    ByMykelCategory? Category,
    ByMykelPattern? Pattern,
    [property: JsonPropertyName("min_float")] float? MinFloat,
    [property: JsonPropertyName("max_float")] float? MaxFloat,
    ByMykelRarity? Rarity,
    bool Stattrak,
    bool Souvenir,
    [property: JsonPropertyName("paint_index")] string? PaintIndex,
    IReadOnlyList<ByMykelWear>? Wears,
    IReadOnlyList<ByMykelCollection>? Collections,
    IReadOnlyList<ByMykelCrateRef>? Crates,
    ByMykelTeam? Team,
    string? Image
);

public record ByMykelSticker(
    string? Id,
    string? Name,
    string? Description,
    ByMykelRarity? Rarity,
    string? Type,
    string? Effect,
    [property: JsonPropertyName("market_hash_name")] string? MarketHashName,
    ByMykelTournament? Tournament,
    IReadOnlyList<ByMykelCollection>? Collections,
    IReadOnlyList<ByMykelCrateRef>? Crates,
    string? Image
);

public record ByMykelKeychain(
    string? Id,
    string? Name,
    string? Description,
    ByMykelRarity? Rarity,
    [property: JsonPropertyName("market_hash_name")] string? MarketHashName,
    IReadOnlyList<ByMykelCollection>? Collections,
    string? Image
);

public record ByMykelCrate(
    string? Id,
    string? Name,
    string? Description,
    string? Type,
    [property: JsonPropertyName("first_sale_date")] string? FirstSaleDate,
    ByMykelRarity? Rarity,
    [property: JsonPropertyName("market_hash_name")] string? MarketHashName,
    bool Rental,
    string? Image
);

public record ByMykelKey(
    string? Id,
    string? Name,
    string? Description,
    [property: JsonPropertyName("market_hash_name")] string? MarketHashName,
    bool Marketable,
    IReadOnlyList<ByMykelCrateRef>? Crates,
    string? Image
);

public record ByMykelCollectibleOriginal(
    [property: JsonPropertyName("item_name")] string? ItemName,
    [property: JsonPropertyName("image_inventory")] string? ImageInventory
);

public record ByMykelCollectible(
    string? Id,
    string? Name,
    string? Description,
    [property: JsonPropertyName("def_index")] string? DefIndex,
    ByMykelRarity? Rarity,
    string? Type,
    bool Genuine,
    [property: JsonPropertyName("market_hash_name")] string? MarketHashName,
    string? Image,
    ByMykelCollectibleOriginal? Original
);

public record ByMykelAgent(
    string? Id,
    string? Name,
    string? Description,
    ByMykelRarity? Rarity,
    ByMykelTeam? Team,
    [property: JsonPropertyName("market_hash_name")] string? MarketHashName,
    IReadOnlyList<ByMykelCollection>? Collections,
    string? Image
);

public record ByMykelPatch(
    string? Id,
    string? Name,
    string? Description,
    [property: JsonPropertyName("def_index")] string? DefIndex,
    ByMykelRarity? Rarity,
    [property: JsonPropertyName("market_hash_name")] string? MarketHashName,
    string? Image
);

public record ByMykelGraffiti(
    string? Id,
    string? Name,
    string? Description,
    ByMykelRarity? Rarity,
    [property: JsonPropertyName("market_hash_name")] string? MarketHashName,
    IReadOnlyList<ByMykelCrateRef>? Crates,
    string? Image
);

public record ByMykelMusicKit(
    string? Id,
    string? Name,
    string? Description,
    [property: JsonPropertyName("def_index")] string? DefIndex,
    ByMykelRarity? Rarity,
    [property: JsonPropertyName("market_hash_name")] string? MarketHashName,
    bool Exclusive,
    string? Image
);
