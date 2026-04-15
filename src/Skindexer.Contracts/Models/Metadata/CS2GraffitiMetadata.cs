namespace Skindexer.Contracts.Models.Metadata;

public class CS2GraffitiMetadata
{
    public string? Rarity { get; init; }
    public string? RarityColor { get; init; }

    public Dictionary<string, object?> ToDictionary() => new()
    {
        [nameof(Rarity)] = Rarity,
        [nameof(RarityColor)] = RarityColor
    };
}
