using Microsoft.EntityFrameworkCore;
using Skindexer.Api.Data;
using Skindexer.Api.Data.Entities;
using Skindexer.Api.Features.Enrichment;
using Skindexer.Api.Tests.Data.Repositories.Fixtures;

namespace Skindexer.Api.Tests.Data.Enrichment;

public class CS2ItemEnricherTests : IClassFixture<PostgresFixture>
{
    private readonly PostgresFixture _fixture;

    public CS2ItemEnricherTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    #region Test Data Builders

    private SkindexerDbContext CreateDb() => new(_fixture.Options);

    private static SkinItemEntity BuildItem(
        string itemType,
        string? rarity = "Mil-Spec",
        string? collection = null)
    {
        var metadata = new Dictionary<string, object?>();

        if (rarity is not null)
            metadata["Rarity"] = rarity;

        if (collection is not null)
            metadata["Collection"] = collection;

        return new SkinItemEntity
        {
            Id = Guid.NewGuid(),
            GameId = "cs2",
            ItemType = itemType,
            Slug = Guid.NewGuid().ToString(),
            Name = Guid.NewGuid().ToString(),
            Metadata = metadata,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    private async Task SeedItemsAsync(SkindexerDbContext db, IEnumerable<SkinItemEntity> items)
    {
        db.Items.AddRange(items);
        await db.SaveChangesAsync();
    }

    private async Task CleanDbAsync(SkindexerDbContext db)
    {
        db.Items.RemoveRange(db.Items);
        db.Collections.RemoveRange(db.Collections);
        db.Rarities.RemoveRange(db.Rarities);
        db.RarityGroups.RemoveRange(db.RarityGroups);
        await db.SaveChangesAsync();
    }

    #endregion

    #region Rarity Group

    [Fact]
    public async Task EnrichAsync_WhenItemsHaveRarity_CreatesRarityGroupForItemType()
    {
        await using var db = CreateDb();
        await CleanDbAsync(db);

        var item = BuildItem(CS2ItemTypes.WeaponSkin, rarity: "Mil-Spec");
        await SeedItemsAsync(db, new List<SkinItemEntity> { item });

        var enricher = new CS2ItemEnricher(db);
        await enricher.EnrichAsync();

        var rarityGroup = await db.RarityGroups
            .FirstOrDefaultAsync(g => g.GameId == "cs2" && g.Type == CS2ItemTypes.WeaponSkin);

        Assert.NotNull(rarityGroup);
        Assert.Equal(CS2ItemTypes.WeaponSkin, rarityGroup.Type);
    }

    [Fact]
    public async Task EnrichAsync_WhenRunTwice_DoesNotDuplicateRarityGroup()
    {
        await using var db = CreateDb();
        await CleanDbAsync(db);

        var item = BuildItem(CS2ItemTypes.WeaponSkin, rarity: "Mil-Spec");
        await SeedItemsAsync(db, new List<SkinItemEntity> { item });

        var enricher = new CS2ItemEnricher(db);
        await enricher.EnrichAsync();
        await enricher.EnrichAsync();

        var groupCount = await db.RarityGroups
            .CountAsync(g => g.GameId == "cs2" && g.Type == CS2ItemTypes.WeaponSkin);

        Assert.Equal(1, groupCount);
    }

    #endregion

    #region Rarities

    [Fact]
    public async Task EnrichAsync_WhenItemsHaveRarity_CreatesRarityRow()
    {
        await using var db = CreateDb();
        await CleanDbAsync(db);

        var item = BuildItem(CS2ItemTypes.WeaponSkin, rarity: "Mil-Spec");
        await SeedItemsAsync(db, new List<SkinItemEntity> { item });

        var enricher = new CS2ItemEnricher(db);
        await enricher.EnrichAsync();

        var rarityGroup = await db.RarityGroups
            .FirstAsync(g => g.GameId == "cs2" && g.Type == CS2ItemTypes.WeaponSkin);

        var rarity = await db.Rarities
            .FirstOrDefaultAsync(r => r.RarityGroupId == rarityGroup.Id && r.Slug == "mil-spec");

        Assert.NotNull(rarity);
        Assert.Equal("Mil-Spec", rarity.Name);
    }

    [Fact]
    public async Task EnrichAsync_WhenRunTwice_DoesNotDuplicateRarities()
    {
        await using var db = CreateDb();
        await CleanDbAsync(db);

        var item = BuildItem(CS2ItemTypes.WeaponSkin, rarity: "Mil-Spec");
        await SeedItemsAsync(db, new List<SkinItemEntity> { item });

        var enricher = new CS2ItemEnricher(db);
        await enricher.EnrichAsync();
        await enricher.EnrichAsync();

        var rarityGroup = await db.RarityGroups
            .FirstAsync(g => g.GameId == "cs2" && g.Type == CS2ItemTypes.WeaponSkin);

        var rarityCount = await db.Rarities
            .CountAsync(r => r.RarityGroupId == rarityGroup.Id && r.Slug == "mil-spec");

        Assert.Equal(1, rarityCount);
    }

    [Fact]
    public async Task EnrichAsync_WhenRunTwice_DoesNotOverwriteManuallySetOrder()
    {
        await using var db = CreateDb();
        await CleanDbAsync(db);

        var item = BuildItem(CS2ItemTypes.WeaponSkin, rarity: "Mil-Spec");
        await SeedItemsAsync(db, new List<SkinItemEntity> { item });

        var enricher = new CS2ItemEnricher(db);
        await enricher.EnrichAsync();

        var rarityGroup = await db.RarityGroups
            .FirstAsync(g => g.GameId == "cs2" && g.Type == CS2ItemTypes.WeaponSkin);

        var rarity = await db.Rarities
            .FirstAsync(r => r.RarityGroupId == rarityGroup.Id && r.Slug == "mil-spec");

        rarity.Order = 3;
        await db.SaveChangesAsync();

        await enricher.EnrichAsync();

        await db.Entry(rarity).ReloadAsync();

        Assert.Equal(3, rarity.Order);
    }

    [Fact]
    public async Task EnrichAsync_WhenItemRarityMissingFromMetadata_SkipsItem()
    {
        await using var db = CreateDb();
        await CleanDbAsync(db);

        var item = BuildItem(CS2ItemTypes.WeaponSkin, rarity: null);
        await SeedItemsAsync(db, new List<SkinItemEntity> { item });

        var enricher = new CS2ItemEnricher(db);
        await enricher.EnrichAsync();

        await db.Entry(item).ReloadAsync();

        Assert.Null(item.RarityId);
    }

    #endregion

    #region Collections

    [Fact]
    public async Task EnrichAsync_WhenItemTypeHasCollection_CreatesCollectionRow()
    {
        await using var db = CreateDb();
        await CleanDbAsync(db);

        var item = BuildItem(CS2ItemTypes.WeaponSkin, collection: "The Dust Collection");
        await SeedItemsAsync(db, new List<SkinItemEntity> { item });

        var enricher = new CS2ItemEnricher(db);
        await enricher.EnrichAsync();

        var collection = await db.Collections
            .FirstOrDefaultAsync(c => c.GameId == "cs2"
                && c.Type == CS2ItemTypes.WeaponSkin
                && c.Slug == "the_dust_collection");

        Assert.NotNull(collection);
        Assert.Equal("The Dust Collection", collection.Name);
    }

    [Fact]
    public async Task EnrichAsync_WhenItemTypeHasNoCollection_DoesNotCreateCollection()
    {
        await using var db = CreateDb();
        await CleanDbAsync(db);

        var item = BuildItem(CS2ItemTypes.Container, collection: null);
        await SeedItemsAsync(db, new List<SkinItemEntity> { item });

        var enricher = new CS2ItemEnricher(db);
        await enricher.EnrichAsync();

        var collectionCount = await db.Collections
            .CountAsync(c => c.GameId == "cs2" && c.Type == CS2ItemTypes.Container);

        Assert.Equal(0, collectionCount);
    }

    [Fact]
    public async Task EnrichAsync_WhenRunTwice_DoesNotDuplicateCollections()
    {
        await using var db = CreateDb();
        await CleanDbAsync(db);

        var item = BuildItem(CS2ItemTypes.WeaponSkin, collection: "The Dust Collection");
        await SeedItemsAsync(db, new List<SkinItemEntity> { item });

        var enricher = new CS2ItemEnricher(db);
        await enricher.EnrichAsync();
        await enricher.EnrichAsync();

        var collectionCount = await db.Collections
            .CountAsync(c => c.GameId == "cs2"
                && c.Type == CS2ItemTypes.WeaponSkin
                && c.Slug == "the_dust_collection");

        Assert.Equal(1, collectionCount);
    }

    #endregion

    #region FK Assignment

    [Fact]
    public async Task EnrichAsync_SetsRarityIdOnItem()
    {
        await using var db = CreateDb();
        await CleanDbAsync(db);

        var item = BuildItem(CS2ItemTypes.WeaponSkin, rarity: "Mil-Spec");
        await SeedItemsAsync(db, new List<SkinItemEntity> { item });

        var enricher = new CS2ItemEnricher(db);
        await enricher.EnrichAsync();

        await db.Entry(item).ReloadAsync();

        Assert.NotNull(item.RarityId);
    }

    [Fact]
    public async Task EnrichAsync_SetsCollectionIdOnItem()
    {
        await using var db = CreateDb();
        await CleanDbAsync(db);

        var item = BuildItem(CS2ItemTypes.WeaponSkin, collection: "The Dust Collection");
        await SeedItemsAsync(db, new List<SkinItemEntity> { item });

        var enricher = new CS2ItemEnricher(db);
        await enricher.EnrichAsync();

        await db.Entry(item).ReloadAsync();

        Assert.NotNull(item.CollectionId);
    }

    [Fact]
    public async Task EnrichAsync_WhenItemTypeIsEmpty_SkipsItem()
    {
        await using var db = CreateDb();
        await CleanDbAsync(db);

        var item = BuildItem(string.Empty, rarity: "Mil-Spec");
        await SeedItemsAsync(db, new List<SkinItemEntity> { item });

        var enricher = new CS2ItemEnricher(db);
        await enricher.EnrichAsync();

        await db.Entry(item).ReloadAsync();

        Assert.Null(item.RarityId);
    }

    #endregion
}