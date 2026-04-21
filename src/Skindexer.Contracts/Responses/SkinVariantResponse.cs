namespace Skindexer.Contracts.Responses;

public class SkinVariantResponse
{
    public Guid Id { get; init; }
    public Guid ItemId { get; init; }
    public string Slug { get; init; } = default!;
    public Dictionary<string, object?> Metadata { get; init; } = [];
}