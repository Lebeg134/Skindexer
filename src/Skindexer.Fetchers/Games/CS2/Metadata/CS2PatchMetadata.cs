namespace Skindexer.Fetchers.Games.CS2.Metadata;

public class CS2PatchMetadata
{
    public string? Rarity { get; init; }
    public string? RarityColor { get; init; }

    public Dictionary<string, object?> ToDictionary() => new()
    {
        [nameof(Rarity)] = Rarity,
        [nameof(RarityColor)] = RarityColor
    };
}
