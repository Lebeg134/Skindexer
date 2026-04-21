namespace Skindexer.Contracts.Responses;

public class RarityGroupResponse
{
    public Guid Id { get; init; }
    public string Slug { get; init; } = default!;
    public string DisplayName { get; init; } = default!;
}