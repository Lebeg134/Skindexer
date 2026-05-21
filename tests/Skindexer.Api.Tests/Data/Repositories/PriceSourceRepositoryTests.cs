using Microsoft.EntityFrameworkCore;
using Skindexer.Api.Data;
using Skindexer.Api.Data.Entities;
using Skindexer.Api.Features.PriceSources;
using Skindexer.Api.Tests.Data.Repositories.Fixtures;
using Skindexer.Contracts.Constants;
using Skindexer.Contracts.Models;

namespace Skindexer.Api.Tests.Data.Repositories;

public class PriceSourceRepositoryTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>, IAsyncLifetime
{
    private SkindexerDbContext _db = null!;
    private PriceSourceRepository _repository = null!;

    public async Task InitializeAsync()
    {
        _db = new SkindexerDbContext(fixture.Options);
        await _db.CurrentPrices.ExecuteDeleteAsync();
        await _db.Variants.ExecuteDeleteAsync();
        await _db.Items.ExecuteDeleteAsync();

        _repository = new PriceSourceRepository(_db);
    }

    public async Task DisposeAsync()
    {
        await _db.DisposeAsync();
    }

    #region Test Data Builders

    private static SkinItemEntity BuildItem(
        string slug = "ak-47-redline",
        string gameId = GameIds.CounterStrike,
        string itemType = CS2ItemTypes.WeaponSkin) => new()
    {
        Id = Guid.NewGuid(),
        GameId = gameId,
        Slug = slug,
        Name = slug,
        ItemType = itemType,
        IsTradeable = true,
        IsMarketable = true,
        Metadata = [],
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
    };

    private static SkinVariantEntity BuildVariant(Guid itemId, string slug, string gameId = GameIds.CounterStrike) => new()
    {
        Id = Guid.NewGuid(),
        ItemId = itemId,
        GameId = gameId,
        Slug = slug,
        Metadata = [],
    };

    private static CurrentPriceEntity BuildCurrentPrice(
        Guid variantId,
        string gameId = GameIds.CounterStrike,
        string slug = "ak-47-redline-field-tested",
        string source = Sources.CS2ShBuff,
        string priceType = PriceTypes.LowestListing,
        decimal price = 10.00m) => new()
    {
        VariantId = variantId,
        GameId = gameId,
        Slug = slug,
        Source = source,
        PriceType = priceType,
        Price = price,
        Currency = "USD",
        RecordedAt = DateTime.UtcNow,
    };

    private static PriceSourceQueryParams NoFilter() => new();
    private static PriceSourceQueryParams WithItemType(string itemType) => new() { ItemType = itemType };

    #endregion

    #region Guard Clauses

    [Fact]
    public async Task GetPriceSourcesAsync_NoPrices_ReturnsEmpty()
    {
        var result = await _repository.GetPriceSourcesAsync(GameIds.CounterStrike, NoFilter());

        Assert.Empty(result);
    }

    #endregion

    #region Correctness

    [Fact]
    public async Task GetPriceSourcesAsync_SingleSource_ReturnsSingleResponse()
    {
        var item = BuildItem();
        var variant = BuildVariant(item.Id, "ak-47-redline-field-tested");
        var price = BuildCurrentPrice(variant.Id);

        _db.Items.Add(item);
        _db.Variants.Add(variant);
        _db.CurrentPrices.Add(price);
        await _db.SaveChangesAsync();

        var result = await _repository.GetPriceSourcesAsync(GameIds.CounterStrike, NoFilter());

        Assert.Single(result);
        Assert.Equal(Sources.CS2ShBuff, result[0].Id);
        Assert.Contains(PriceTypes.LowestListing, result[0].PriceTypes);
    }

    [Fact]
    public async Task GetPriceSourcesAsync_MultiplePriceTypesForSameSource_GroupedIntoOneResponse()
    {
        var item = BuildItem();
        var variant = BuildVariant(item.Id, "ak-47-redline-field-tested");

        _db.Items.Add(item);
        _db.Variants.Add(variant);
        _db.CurrentPrices.AddRange(
            BuildCurrentPrice(variant.Id, priceType: PriceTypes.LowestListing),
            BuildCurrentPrice(variant.Id, priceType: PriceTypes.BuyOrder)
        );
        await _db.SaveChangesAsync();

        var result = await _repository.GetPriceSourcesAsync(GameIds.CounterStrike, NoFilter());

        Assert.Single(result);
        Assert.Equal(2, result[0].PriceTypes.Count);
        Assert.Contains(PriceTypes.LowestListing, result[0].PriceTypes);
        Assert.Contains(PriceTypes.BuyOrder, result[0].PriceTypes);
    }

    [Fact]
    public async Task GetPriceSourcesAsync_MultipleSources_ReturnsAll()
    {
        var item = BuildItem();
        var variant = BuildVariant(item.Id, "ak-47-redline-field-tested");

        _db.Items.Add(item);
        _db.Variants.Add(variant);
        _db.CurrentPrices.AddRange(
            BuildCurrentPrice(variant.Id, source: Sources.CS2ShBuff),
            BuildCurrentPrice(variant.Id, source: Sources.CS2ShSteam)
        );
        await _db.SaveChangesAsync();

        var result = await _repository.GetPriceSourcesAsync(GameIds.CounterStrike, NoFilter());

        Assert.Equal(2, result.Count);
        Assert.Contains(result, r => r.Id == Sources.CS2ShBuff);
        Assert.Contains(result, r => r.Id == Sources.CS2ShSteam);
    }

    [Fact]
    public async Task GetPriceSourcesAsync_FiltersByGameId()
    {
        var cs2Item = BuildItem(gameId: GameIds.CounterStrike);
        var cs2Variant = BuildVariant(cs2Item.Id, "ak-47-redline-field-tested", GameIds.CounterStrike);

        var rustItem = BuildItem(slug: "rust-ak", gameId: "rust");
        var rustVariant = BuildVariant(rustItem.Id, "rust-ak", "rust");

        _db.Items.AddRange(cs2Item, rustItem);
        _db.Variants.AddRange(cs2Variant, rustVariant);
        _db.CurrentPrices.AddRange(
            BuildCurrentPrice(cs2Variant.Id, gameId: GameIds.CounterStrike),
            BuildCurrentPrice(rustVariant.Id, gameId: "rust", slug: "rust-ak", source: Sources.SteamAnalyst)
        );
        await _db.SaveChangesAsync();

        var result = await _repository.GetPriceSourcesAsync(GameIds.CounterStrike, NoFilter());

        Assert.Single(result);
        Assert.Equal(Sources.CS2ShBuff, result[0].Id);
    }

    #endregion

    #region ItemType Filter

    [Fact]
    public async Task GetPriceSourcesAsync_ItemTypeFilter_ExcludesOtherTypes()
    {
        var weaponItem = BuildItem(slug: "ak-47-redline", itemType: CS2ItemTypes.WeaponSkin);
        var weaponVariant = BuildVariant(weaponItem.Id, "ak-47-redline-field-tested");

        var stickerItem = BuildItem(slug: "howl-sticker", itemType: CS2ItemTypes.Sticker);
        var stickerVariant = BuildVariant(stickerItem.Id, "howl-sticker");

        _db.Items.AddRange(weaponItem, stickerItem);
        _db.Variants.AddRange(weaponVariant, stickerVariant);
        _db.CurrentPrices.AddRange(
            BuildCurrentPrice(weaponVariant.Id, source: Sources.CS2ShBuff),
            BuildCurrentPrice(stickerVariant.Id, slug: "howl-sticker", source: Sources.SteamAnalyst)
        );
        await _db.SaveChangesAsync();

        var result = await _repository.GetPriceSourcesAsync(GameIds.CounterStrike, WithItemType(CS2ItemTypes.WeaponSkin));

        Assert.Single(result);
        Assert.Equal(Sources.CS2ShBuff, result[0].Id);
    }

    [Fact]
    public async Task GetPriceSourcesAsync_ItemTypeFilter_NoMatch_ReturnsEmpty()
    {
        var item = BuildItem(itemType: CS2ItemTypes.WeaponSkin);
        var variant = BuildVariant(item.Id, "ak-47-redline-field-tested");
        var price = BuildCurrentPrice(variant.Id);

        _db.Items.Add(item);
        _db.Variants.Add(variant);
        _db.CurrentPrices.Add(price);
        await _db.SaveChangesAsync();

        var result = await _repository.GetPriceSourcesAsync(GameIds.CounterStrike, WithItemType(CS2ItemTypes.Sticker));

        Assert.Empty(result);
    }

    #endregion
}
