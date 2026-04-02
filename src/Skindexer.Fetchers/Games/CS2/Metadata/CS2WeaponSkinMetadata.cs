namespace Skindexer.Fetchers.Games.CS2.Metadata;

public class CS2WeaponSkinMetadata
{
    public string? WeaponId { get; init; }
    public string? WeaponName { get; init; }
    public string? Rarity { get; init; }
    public string? RarityColor { get; init; }
    public string? Category { get; init; }
    public string? Collection { get; init; }
    public IReadOnlyList<string> AvailableWears { get; init; } = [];
    public float? MinFloat { get; init; }
    public float? MaxFloat { get; init; }
    public string? PaintIndex { get; init; }
    public string? PatternName { get; init; }
    public string? Description { get; init; }

    public Dictionary<string, object?> ToDictionary() => new()
    {
        [nameof(WeaponId)] = WeaponId,
        [nameof(WeaponName)] = WeaponName,
        [nameof(Rarity)] = Rarity,
        [nameof(RarityColor)] = RarityColor,
        [nameof(Category)] = Category,
        [nameof(Collection)] = Collection,
        [nameof(AvailableWears)] = AvailableWears,
        [nameof(MinFloat)] = MinFloat,
        [nameof(MaxFloat)] = MaxFloat,
        [nameof(PaintIndex)] = PaintIndex,
        [nameof(PatternName)] = PatternName,
        [nameof(Description)] = Description,
    };
}