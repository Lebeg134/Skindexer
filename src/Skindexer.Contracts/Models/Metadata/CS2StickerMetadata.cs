namespace Skindexer.Contracts.Models.Metadata;

public class CS2StickerMetadata
{
    public string? Rarity { get; init; }
    public string? RarityColor { get; init; }
    public string? Effect { get; init; }       // Foil, Holo, Gold, etc.
    public string? TournamentName { get; init; }
    public string? Collection { get; init; }

    public Dictionary<string, object?> ToDictionary() => new()
    {
        [nameof(Rarity)]         = Rarity,
        [nameof(RarityColor)]    = RarityColor,
        [nameof(Effect)]         = Effect,
        [nameof(TournamentName)] = TournamentName,
        [nameof(Collection)]     = Collection,
    };
}
