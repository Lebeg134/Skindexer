namespace Skindexer.Api.Data.Entities;

public class RarityEntity
{
    public Guid Id { get; set; }
    public string GameId { get; set; } = default!;
    public string Slug { get; set; } = default!;
    public string Name { get; set; } = default!;
    public int? Order { get; set; }

    public Guid? RarityGroupId { get; set; }
    public RarityGroupEntity? RarityGroup { get; set; }

    public ICollection<SkinItemEntity> Items { get; set; } = [];
}