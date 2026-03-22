namespace Skindexer.Contracts.Responses;

public class CollectionResponse
{
    public string Slug { get; init; } = default!;
    public string DisplayName { get; init; } = default!;
    public IReadOnlyList<string> ItemSlugs { get; init; } = [];
}