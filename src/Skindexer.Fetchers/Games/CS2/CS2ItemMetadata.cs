namespace Skindexer.Fetchers.Games.CS2;

public class CS2ItemMetadata
{
    public string? Collection { get; init; }
    public string? Rarity { get; init; }
    public string? Exterior { get; init; }
    public bool StatTrak { get; init; }
    public bool Souvenir { get; init; }
    public string? Type { get; init; }        // "Rifle", "Knife", "Sticker" etc
    public string? WeaponName { get; init; }  // "AK-47", "M4A4" etc

    public Dictionary<string, object?> ToDictionary() => new()
    {
        [nameof(Collection)] = Collection,
        [nameof(Rarity)] = Rarity,
        [nameof(Exterior)] = Exterior,
        [nameof(StatTrak)] = StatTrak,
        [nameof(Souvenir)] = Souvenir,
        [nameof(Type)] = Type,
        [nameof(WeaponName)] = WeaponName
    };
}
