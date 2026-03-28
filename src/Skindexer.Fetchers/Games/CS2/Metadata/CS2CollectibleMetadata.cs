namespace Skindexer.Fetchers.Games.CS2.Metadata;

public class CS2CollectibleMetadata
{
    public string? Rarity { get; init; }
    public string? RarityColor { get; init; }
    public string? Type { get; init; }
    public bool Genuine { get; init; }
    public string? DefIndex { get; init; }

    public Dictionary<string, object?> ToDictionary() => new()
    {
        [nameof(Rarity)] = Rarity,
        [nameof(RarityColor)] = RarityColor,
        [nameof(Type)] = Type,
        [nameof(Genuine)] = Genuine,
        [nameof(DefIndex)] = DefIndex,
    };
}