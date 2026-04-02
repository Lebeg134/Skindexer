namespace Skindexer.Api.Data.Entities;

public class SkinVariantEntity
{
    public Guid Id { get; set; }
    public Guid ItemId { get; set; }
    public string GameId { get; set; } = default!;
    public string Slug { get; set; } = default!;
    public Dictionary<string, object?> Metadata { get; set; } = [];

    public SkinItemEntity Item { get; set; } = default!;
    public ICollection<SkinPriceEntity> Prices { get; set; } = [];
}