namespace Skindexer.Contracts.Models;

public class SkinVariant
{
    public Guid Id { get; init; }
    public Guid ItemId { get; init; }
    public string GameId { get; init; } = default!;
    public string Slug { get; init; } = default!;
    public Dictionary<string, object?> Metadata { get; init; } = new();
}