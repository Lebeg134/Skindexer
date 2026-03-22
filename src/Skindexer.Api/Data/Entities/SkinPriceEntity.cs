namespace Skindexer.Api.Data.Entities;

public class SkinPriceEntity
{
    public Guid Id { get; set; }
    public Guid ItemId { get; set; }
    public string Slug { get; set; } = default!;
    public string Source { get; set; } = default!;
    public string PriceType { get; set; } = default!;
    public decimal Price { get; set; }
    public string Currency { get; set; } = default!;
    public int? Volume { get; set; }
    public DateTime RecordedAt { get; set; }

    public SkinItemEntity Item { get; set; } = default!;
}