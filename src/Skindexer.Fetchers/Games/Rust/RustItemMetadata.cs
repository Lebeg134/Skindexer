namespace Skindexer.Fetchers.Games.Rust;

public class RustItemMetadata
{
    public string? Category { get; init; }     // "Weapon", "Clothing", "Tool" etc
    public string? Subcategory { get; init; }
    public int? CraftCount { get; init; }
    public string? Workshop { get; init; }     // Workshop submission ID if any

    public Dictionary<string, object?> ToDictionary() => new()
    {
        [nameof(Category)] = Category,
        [nameof(Subcategory)] = Subcategory,
        [nameof(CraftCount)] = CraftCount,
        [nameof(Workshop)] = Workshop
    };
}
