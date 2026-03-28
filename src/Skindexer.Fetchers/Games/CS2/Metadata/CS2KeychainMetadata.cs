namespace Skindexer.Fetchers.Games.CS2.Metadata;

public class CS2KeychainMetadata
{
    public string? Rarity { get; init; }
    public string? RarityColor { get; init; }
    public string? Collection { get; init; }

    public Dictionary<string, object?> ToDictionary() => new()
    {
        [nameof(Rarity)]      = Rarity,
        [nameof(RarityColor)] = RarityColor,
        [nameof(Collection)]  = Collection,
    };
}