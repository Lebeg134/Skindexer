namespace Skindexer.Api.Data.Entities;

public class CurrentPriceEntity
{
    public Guid VariantId { get; set; }
    public string GameId { get; set; } = default!;
    public string Slug { get; set; } = default!;
    public string Source { get; set; } = default!;
    public string PriceType { get; set; } = default!;
    public decimal Price { get; set; }
    public string Currency { get; set; } = default!;
    public int? Volume { get; set; }
    public DateTime RecordedAt { get; set; }
}