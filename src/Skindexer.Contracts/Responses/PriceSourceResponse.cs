namespace Skindexer.Contracts.Responses;

public record PriceSourceResponse(
    string Id,
    string? Name,
    IReadOnlyList<string> PriceTypes
);