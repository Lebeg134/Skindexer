namespace Skindexer.Contracts.Models;

public class SkinItem
{
    public Guid Id { get; init; }
    public string GameId { get; init; } = default!;
    public string Slug { get; init; } = default!;
    public string Name { get; init; } = default!;
    public string? ImageUrl { get; init; }
    public bool IsTradeable { get; init; }
    public bool IsMarketable { get; init; }
    public Dictionary<string, object?> Metadata { get; init; } = new();
    public DateTime? AddedToGameAt { get; set; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}
