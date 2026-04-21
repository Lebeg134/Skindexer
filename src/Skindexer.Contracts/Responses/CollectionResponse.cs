namespace Skindexer.Contracts.Responses;

public class CollectionResponse
{
    public Guid Id { get; init; }
    public string Slug { get; init; } = default!;
    public string DisplayName { get; init; } = default!;
    public IReadOnlyList<Guid> ItemIds { get; init; } = [];
}