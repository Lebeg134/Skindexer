namespace Skindexer.Contracts.Responses;

public class RarityResponse
{
    public string Slug { get; init; } = default!;
    public string DisplayName { get; init; } = default!;
    public int? Order { get; init; }
    public string? GroupSlug { get; init; } = default!;
}