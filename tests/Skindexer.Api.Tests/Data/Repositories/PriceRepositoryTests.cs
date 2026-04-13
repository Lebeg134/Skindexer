using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using Skindexer.Api.Data;
using Skindexer.Api.Data.Entities;
using Skindexer.Api.Data.Repositories;
using Skindexer.Api.Tests.Data.Repositories.Fixtures;
using Skindexer.Contracts.Models;
using Testcontainers.PostgreSql;

namespace Skindexer.Api.Tests.Data.Repositories;

public class PriceRepositoryTests(PostgresFixture fixture)
    : IClassFixture<PostgresFixture>, IAsyncLifetime
{
    private SkindexerDbContext _db = null!;
    private PriceRepository _repository = null!;

    public async Task InitializeAsync()
    {
        _db = new SkindexerDbContext(fixture.Options);
        await _db.Prices.ExecuteDeleteAsync();
        await _db.Variants.ExecuteDeleteAsync();
        await _db.Items.ExecuteDeleteAsync();

        _repository = new PriceRepository(_db, fixture.DataSource, NullLogger<PriceRepository>.Instance);
    }

    public async Task DisposeAsync()
    {
        await _db.DisposeAsync();
    }

    #region Test Data Builders

    private async Task<SkinItemEntity> SeedItemAsync(
        string slug = "ak-47-redline",
        string gameId = "cs2")
    {
        var item = new SkinItemEntity
        {
            Id = Guid.NewGuid(),
            GameId = gameId,
            Slug = slug,
            Name = slug,
            IsTradeable = true,
            IsMarketable = true,
            Metadata = new Dictionary<string, object?>(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        _db.Items.Add(item);
        await _db.SaveChangesAsync();
        return item;
    }

    private async Task<SkinVariantEntity> SeedVariantAsync(
        SkinItemEntity item,
        string? slug = null)
    {
        var variant = new SkinVariantEntity
        {
            Id = Guid.NewGuid(),
            ItemId = item.Id,
            GameId = item.GameId,
            Slug = slug ?? item.Slug,
            Metadata = new Dictionary<string, object?>(),
        };

        _db.Variants.Add(variant);
        await _db.SaveChangesAsync();
        return variant;
    }

    private static SkinPrice BuildPrice(
        Guid variantId,
        string slug,
        decimal price = 10m,
        int? volume = 100,
        string source = "kaggle-steam",
        DateTime? recordedAt = null) => new()
    {
        VariantId = variantId,
        Slug = slug,
        Source = source,
        PriceType = "median_daily",
        Price = price,
        Currency = "USD",
        Volume = volume,
        RecordedAt = recordedAt ?? new DateTime(2021, 1, 1, 0, 0, 0, DateTimeKind.Utc),
    };

    #endregion

    #region GetCurrentPricesByGameAsync

    [Fact]
    public async Task GetCurrentPricesByGameAsync_NoItems_ReturnsEmpty()
    {
        var result = await _repository.GetCurrentPricesByGameAsync("cs2", CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetCurrentPricesByGameAsync_ReturnsOnlyMostRecentSnapshot()
    {
        var item = await SeedItemAsync();
        var variant = await SeedVariantAsync(item);

        var prices = new List<SkinPrice>
        {
            BuildPrice(variant.Id, variant.Slug, price: 10m, recordedAt: new DateTime(2021, 1, 1, 0, 0, 0, DateTimeKind.Utc)),
            BuildPrice(variant.Id, variant.Slug, price: 11m, recordedAt: new DateTime(2021, 1, 2, 0, 0, 0, DateTimeKind.Utc)),
            BuildPrice(variant.Id, variant.Slug, price: 12m, recordedAt: new DateTime(2021, 1, 3, 0, 0, 0, DateTimeKind.Utc)),
        };

        await _repository.InsertPricesAsync(prices, CancellationToken.None);

        var result = await _repository.GetCurrentPricesByGameAsync("cs2", CancellationToken.None);

        Assert.Single(result);
        Assert.Equal(12m, result[0].Price);
    }

    [Fact]
    public async Task GetCurrentPricesByGameAsync_MultipleItems_ReturnsMostRecentPerItem()
    {
        var item1 = await SeedItemAsync(slug: "ak-47-redline");
        var item2 = await SeedItemAsync(slug: "m4a4-howl");
        var variant1 = await SeedVariantAsync(item1);
        var variant2 = await SeedVariantAsync(item2);

        var prices = new List<SkinPrice>
        {
            BuildPrice(variant1.Id, variant1.Slug, price: 10m,
                recordedAt: new DateTime(2021, 1, 1, 0, 0, 0, DateTimeKind.Utc)),
            BuildPrice(variant1.Id, variant1.Slug, price: 15m,
                recordedAt: new DateTime(2021, 1, 2, 0, 0, 0, DateTimeKind.Utc)),
            BuildPrice(variant2.Id, variant2.Slug, price: 500m,
                recordedAt: new DateTime(2021, 1, 1, 0, 0, 0, DateTimeKind.Utc)),
            BuildPrice(variant2.Id, variant2.Slug, price: 550m,
                recordedAt: new DateTime(2021, 1, 2, 0, 0, 0, DateTimeKind.Utc)),
        };

        await _repository.InsertPricesAsync(prices, CancellationToken.None);

        var result = await _repository.GetCurrentPricesByGameAsync("cs2", CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, p => p.VariantId == variant1.Id && p.Price == 15m);
        Assert.Contains(result, p => p.VariantId == variant2.Id && p.Price == 550m);
    }

    [Fact]
    public async Task GetCurrentPricesByGameAsync_MultipleSources_ReturnsMostRecentPerSource()
    {
        var item = await SeedItemAsync();
        var variant = await SeedVariantAsync(item);

        var prices = new List<SkinPrice>
        {
            BuildPrice(variant.Id, variant.Slug, price: 10m,
                recordedAt: new DateTime(2021, 1, 1, 0, 0, 0, DateTimeKind.Utc)),
            BuildPrice(variant.Id, variant.Slug, price: 11m,
                recordedAt: new DateTime(2021, 1, 2, 0, 0, 0, DateTimeKind.Utc),
                source: "steam-market"),
        };

        await _repository.InsertPricesAsync(prices, CancellationToken.None);

        var result = await _repository.GetCurrentPricesByGameAsync("cs2", CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, p => p.Source == "kaggle-steam" && p.Price == 10m);
        Assert.Contains(result, p => p.Source == "steam-market" && p.Price == 11m);
    }

    [Fact]
    public async Task GetCurrentPricesByGameAsync_FiltersByGameId()
    {
        var cs2Item = await SeedItemAsync(slug: "ak-47-redline", gameId: "cs2");
        var tf2Item = await SeedItemAsync(slug: "ak-47-redline", gameId: "tf2");
        var cs2Variant = await SeedVariantAsync(cs2Item);
        var tf2Variant = await SeedVariantAsync(tf2Item);

        var prices = new List<SkinPrice>
        {
            BuildPrice(cs2Variant.Id, cs2Variant.Slug, price: 10m),
            BuildPrice(tf2Variant.Id, tf2Variant.Slug, price: 99m),
        };

        await _repository.InsertPricesAsync(prices, CancellationToken.None);

        var result = await _repository.GetCurrentPricesByGameAsync("cs2", CancellationToken.None);

        Assert.Single(result);
        Assert.Equal(cs2Variant.Id, result[0].VariantId);
    }

    #endregion

    #region Guard Clauses

    [Fact]
    public async Task UpsertPricesAsync_EmptyList_DoesNothing()
    {
        await _repository.UpsertPricesAsync([], CancellationToken.None);

        var count = await _db.Prices.CountAsync();
        Assert.Equal(0, count);
    }

    #endregion

    #region Insert

    [Fact]
    public async Task UpsertPricesAsync_NewPrices_InsertsAllRows()
    {
        var item = await SeedItemAsync();
        var variant = await SeedVariantAsync(item);

        var prices = new List<SkinPrice>
        {
            BuildPrice(variant.Id, variant.Slug, price: 10m, recordedAt: new DateTime(2021, 1, 1, 0, 0, 0, DateTimeKind.Utc)),
            BuildPrice(variant.Id, variant.Slug, price: 11m, recordedAt: new DateTime(2021, 1, 2, 0, 0, 0, DateTimeKind.Utc)),
            BuildPrice(variant.Id, variant.Slug, price: 12m, recordedAt: new DateTime(2021, 1, 3, 0, 0, 0, DateTimeKind.Utc)),
        };

        await _repository.UpsertPricesAsync(prices, CancellationToken.None);

        var stored = await _db.Prices.OrderBy(p => p.RecordedAt).ToListAsync();
        Assert.Equal(3, stored.Count);
        Assert.Equal(10m, stored[0].Price);
        Assert.Equal(11m, stored[1].Price);
        Assert.Equal(12m, stored[2].Price);
    }

    [Fact]
    public async Task UpsertPricesAsync_NullVolume_InsertsWithoutError()
    {
        var item = await SeedItemAsync();
        var variant = await SeedVariantAsync(item);

        var prices = new List<SkinPrice>
        {
            BuildPrice(variant.Id, variant.Slug, volume: null),
        };

        await _repository.UpsertPricesAsync(prices, CancellationToken.None);

        var stored = await _db.Prices.SingleAsync();
        Assert.Null(stored.Volume);
    }

    [Fact]
    public async Task UpsertPricesAsync_MultipleItems_AllPricesLandCorrectly()
    {
        var item1 = await SeedItemAsync(slug: "ak-47-redline");
        var item2 = await SeedItemAsync(slug: "m4a4-howl");
        var variant1 = await SeedVariantAsync(item1);
        var variant2 = await SeedVariantAsync(item2);

        var prices = new List<SkinPrice>
        {
            BuildPrice(variant1.Id, variant1.Slug, price: 10m),
            BuildPrice(variant2.Id, variant2.Slug, price: 500m),
        };

        await _repository.UpsertPricesAsync(prices, CancellationToken.None);

        var stored = await _db.Prices.ToListAsync();
        Assert.Equal(2, stored.Count);
        Assert.Contains(stored, p => p.VariantId == variant1.Id && p.Price == 10m);
        Assert.Contains(stored, p => p.VariantId == variant2.Id && p.Price == 500m);
    }

    #endregion

    #region Upsert Behaviour

    [Fact]
    public async Task UpsertPricesAsync_ReImport_SameData_DoesNotDuplicate()
    {
        var item = await SeedItemAsync();
        var variant = await SeedVariantAsync(item);

        var prices = new List<SkinPrice>
        {
            BuildPrice(variant.Id, variant.Slug),
        };

        await _repository.UpsertPricesAsync(prices, CancellationToken.None);
        await _repository.UpsertPricesAsync(prices, CancellationToken.None);

        var count = await _db.Prices.CountAsync();
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task UpsertPricesAsync_ReImport_SameSnapshot_IsIgnored()
    {
        var item = await SeedItemAsync();
        var variant = await SeedVariantAsync(item);

        var original = new List<SkinPrice>
        {
            BuildPrice(variant.Id, variant.Slug, price: 10m, volume: 100),
        };

        var duplicate = new List<SkinPrice>
        {
            BuildPrice(variant.Id, variant.Slug, price: 15m, volume: 200),
        };

        await _repository.UpsertPricesAsync(original, CancellationToken.None);
        await _repository.UpsertPricesAsync(duplicate, CancellationToken.None);

        var stored = await _db.Prices.SingleAsync();
        Assert.Equal(10m, stored.Price);
        Assert.Equal(100, stored.Volume);
    }

    [Fact]
    public async Task UpsertPricesAsync_DuplicatesInSameBatch_LastOneWins()
    {
        var item = await SeedItemAsync();
        var variant = await SeedVariantAsync(item);
        var recordedAt = new DateTime(2021, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        // Same natural key twice in one batch — in-memory dedup should keep the last
        var prices = new List<SkinPrice>
        {
            BuildPrice(variant.Id, variant.Slug, price: 10m, recordedAt: recordedAt),
            BuildPrice(variant.Id, variant.Slug, price: 99m, recordedAt: recordedAt),
        };

        await _repository.UpsertPricesAsync(prices, CancellationToken.None);

        var stored = await _db.Prices.SingleAsync();
        Assert.Equal(99m, stored.Price);
    }

    #endregion
}