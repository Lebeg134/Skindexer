namespace Skindexer.Fetchers.Games.CS2;

public class CS2ItemMetadata
{
    public string? Collection { get; init; }
    public string? Rarity { get; init; }
    public IReadOnlyList<string> AvailableWears { get; init; } = [];
    public bool StatTrak { get; init; }
    public bool Souvenir { get; init; }
    public string? Type { get; init; }
    public string? WeaponName { get; init; }

    public Dictionary<string, object?> ToDictionary() => new()
    {
        [nameof(Collection)] = Collection,
        [nameof(Rarity)] = Rarity,
        [nameof(AvailableWears)] = AvailableWears,
        [nameof(StatTrak)] = StatTrak,
        [nameof(Souvenir)] = Souvenir,
        [nameof(Type)] = Type,
        [nameof(WeaponName)] = WeaponName
    };
}