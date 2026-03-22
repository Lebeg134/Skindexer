namespace Skindexer.Contracts.Responses;

public class SkinItemResponse
{
    public Guid Id { get; init; }
    public string Slug { get; init; } = default!;
    public string Name { get; init; } = default!;
    public string? ImageUrl { get; init; }
    public bool IsTradeable { get; init; }
    public bool IsMarketable { get; init; }
    public Dictionary<string, object?> Metadata { get; init; } = [];
    public DateTime? AddedToGameAt { get; set; }
}