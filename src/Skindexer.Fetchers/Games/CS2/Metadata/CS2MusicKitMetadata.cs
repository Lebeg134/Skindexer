namespace Skindexer.Fetchers.Games.CS2.Metadata;

public class CS2MusicKitMetadata
{
    public string? Rarity { get; init; }
    public string? RarityColor { get; init; }
    public bool Exclusive { get; init; }

    public Dictionary<string, object?> ToDictionary() => new()
    {
        [nameof(Rarity)]      = Rarity,
        [nameof(RarityColor)] = RarityColor,
        [nameof(Exclusive)]   = Exclusive,
    };
}
