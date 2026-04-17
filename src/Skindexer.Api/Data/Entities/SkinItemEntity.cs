namespace Skindexer.Api.Data.Entities;

public class SkinItemEntity
{
    public Guid Id { get; set; }
    public string GameId { get; set; } = default!;
    public string ItemType { get; set; } = string.Empty;
    public string Slug { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? ImageUrl { get; set; }
    public bool IsTradeable { get; set; }
    public bool IsMarketable { get; set; }
    public Dictionary<string, object?> Metadata { get; set; } = [];
    public DateTime? AddedToGameAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Guid? CollectionId { get; set; }
    public CollectionEntity? Collection { get; set; }

    public Guid? RarityId { get; set; }
    public RarityEntity? Rarity { get; set; }

    public ICollection<SkinVariantEntity> Variants { get; set; } = [];
}