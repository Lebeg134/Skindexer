namespace Skindexer.Fetchers.Games.CS2.Metadata;

public class CS2CrateMetadata
{
    public string? Rarity { get; init; }
    public string? RarityColor { get; init; }
    public string? Type { get; init; }          // Case, Capsule, SouvenirPackage, etc.
    public string? FirstSaleDate { get; init; }
    public bool Rental { get; init; }

    public Dictionary<string, object?> ToDictionary() => new()
    {
        [nameof(Rarity)]         = Rarity,
        [nameof(RarityColor)]    = RarityColor,
        [nameof(Type)]           = Type,
        [nameof(FirstSaleDate)]  = FirstSaleDate,
        [nameof(Rental)]         = Rental,
    };
}