namespace Skindexer.Contracts.Models.Metadata;

public class CS2AgentMetadata
{
    public string? Rarity { get; init; }
    public string? RarityColor { get; init; }
    public string? Team { get; init; }
    public string? Collection { get; init; }

    public Dictionary<string, object?> ToDictionary() => new()
    {
        [nameof(Rarity)]      = Rarity,
        [nameof(RarityColor)] = RarityColor,
        [nameof(Team)]        = Team,
        [nameof(Collection)]  = Collection,
    };
}
