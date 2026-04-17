namespace Skindexer.Api.Data.Entities;

public class RarityGroupEntity
{
    public Guid Id { get; set; }
    public string GameId { get; set; } = default!;
    public string Slug { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string Type { get; set; } = string.Empty;

    public ICollection<RarityEntity> Rarities { get; set; } = [];
}