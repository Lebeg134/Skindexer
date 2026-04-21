namespace Skindexer.Contracts.Responses;

public class SkinPriceResponse
{
    public Guid VariantId { get; init; }
    public string Slug { get; init; } = default!;
    public string Source { get; init; } = default!;
    public string PriceType { get; init; } = default!;
    public decimal Price { get; init; }
    public string Currency { get; init; } = default!;
    public int? Volume { get; init; }
    public DateTime RecordedAt { get; init; }
}