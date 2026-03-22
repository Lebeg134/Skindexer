namespace Skindexer.Contracts.Responses;

public class GradeResponse
{
    public string Slug { get; init; } = default!;
    public string DisplayName { get; init; } = default!;
    public int Order { get; init; }
}