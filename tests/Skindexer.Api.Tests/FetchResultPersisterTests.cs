using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Skindexer.Api.Data;
using Skindexer.Api.Data.Repositories;
using Skindexer.Api.Features;
using Skindexer.Api.Features.Enrichment;
using Skindexer.Api.Tests.Data.Repositories.Fixtures;
using Skindexer.Contracts.Constants;
using Skindexer.Contracts.Models;

namespace Skindexer.Api.Tests.Data;

public class FetchResultPersisterTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>, IAsyncLifetime
{
    private SkindexerDbContext _db = null!;
    private FetchResultPersister _persister = null!;

    public async Task InitializeAsync()
    {
        _db = new SkindexerDbContext(fixture.Options);

        await _db.Prices.ExecuteDeleteAsync();
        await _db.CurrentPrices.ExecuteDeleteAsync();
        await _db.Variants.ExecuteDeleteAsync();
        await _db.Items.ExecuteDeleteAsync();

        var items = new ItemRepository(_db, fixture.DataSource, NullLogger<ItemRepository>.Instance);
        var variants = new VariantRepository(_db);
        var prices = new PriceRepository(_db, fixture.DataSource, NullLogger<PriceRepository>.Instance);
        var enrichers = Enumerable.Empty<IItemEnricher>();

        _persister = new FetchResultPersister(items, variants, prices, enrichers, NullLogger<FetchResultPersister>.Instance);
    }

    public async Task DisposeAsync()
    {
        await _db.DisposeAsync();
    }

    #region Test Data Builders

    private static SkinItem BuildItem(
        string slug = "ak-47-redline",
        string gameId = GameIds.CounterStrike,
        string name = "AK-47 | Redline") => new()
    {
        Id = Guid.NewGuid(),
        GameId = gameId,
        Slug = slug,
        Name = name,
        ItemType = "weapon_skin",
        IsTradeable = true,
        IsMarketable = true,
        Metadata = [],
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
    };

    private static SkinVariant BuildVariant(SkinItem parentItem, string variantSlug) => new()
    {
        Id = Guid.NewGuid(),
        ItemId = parentItem.Id,       // in-memory Id — persister must remap this
        GameId = parentItem.GameId,
        Slug = variantSlug,
        Metadata = [],
    };

    private static SkinPrice BuildPrice(SkinVariant variant, string source = "cs2sh-buff") => new()
    {
        VariantId = variant.Id,       // in-memory Id — persister must remap this too
        GameId = variant.GameId,
        Slug = variant.Slug,
        Source = source,
        PriceType = "lowest_listing",
        Price = 34.30m,
        Currency = "USD",
        Volume = 100,
        RecordedAt = DateTime.UtcNow,
    };

    private static FetchResult BuildCs2ShResult(
        IReadOnlyList<SkinItem> items,
        IReadOnlyList<SkinVariant> variants,
        IReadOnlyList<SkinPrice> prices) => FetchResult.Success(
            gameId: GameIds.CounterStrike,
            source: Sources.CS2Sh,
            items: items,
            variants: variants,
            prices: prices);

    #endregion

    #region Regression — FK Violation (cs2.sh style: items + variants + prices in one result)

    // This is the core regression test for the bug:
    // cs2.sh fetcher populates Items, Variants AND Prices in a single FetchResult,
    // setting VariantId directly on prices using in-memory Guids.
    // The persister must NOT pass those Guids through directly — it must always
    // resolve VariantId via the DB slug map after upserting variants.
    [Fact]
    public async Task PersistAsync_CS2ShStyleResult_DoesNotThrowFkViolation()
    {
        var item = BuildItem(slug: "ak-47-redline");
        var variant = BuildVariant(item, "ak-47-redline-field-tested");
        var price = BuildPrice(variant);

        var items = new List<SkinItem> { item };
        var variants = new List<SkinVariant> { variant };
        var prices = new List<SkinPrice> { price };

        var result = BuildCs2ShResult(items, variants, prices);

        // Must not throw Npgsql.PostgresException 23503 FK violation
        await _persister.PersistAsync(result, CancellationToken.None);
    }

    [Fact]
    public async Task PersistAsync_CS2ShStyleResult_PricesLandInDb()
    {
        var item = BuildItem(slug: "ak-47-redline");
        var variant = BuildVariant(item, "ak-47-redline-field-tested");
        var price = BuildPrice(variant);

        var items = new List<SkinItem> { item };
        var variants = new List<SkinVariant> { variant };
        var prices = new List<SkinPrice> { price };

        var result = BuildCs2ShResult(items, variants, prices);

        await _persister.PersistAsync(result, CancellationToken.None);

        var storedPrice = await _db.CurrentPrices.SingleAsync();
        Assert.Equal("ak-47-redline-field-tested", storedPrice.Slug);
        Assert.Equal(Sources.CS2ShBuff, storedPrice.Source);
        Assert.Equal(34.30m, storedPrice.Price);
    }

    [Fact]
    public async Task PersistAsync_CS2ShStyleResult_PriceVariantIdMatchesDbVariantId()
    {
        var item = BuildItem(slug: "ak-47-redline");
        var variant = BuildVariant(item, "ak-47-redline-field-tested");
        var price = BuildPrice(variant);

        var items = new List<SkinItem> { item };
        var variants = new List<SkinVariant> { variant };
        var prices = new List<SkinPrice> { price };

        var result = BuildCs2ShResult(items, variants, prices);

        await _persister.PersistAsync(result, CancellationToken.None);

        var dbVariant = await _db.Variants.SingleAsync();
        var storedPrice = await _db.CurrentPrices.SingleAsync();

        // The price's VariantId must match the DB-assigned variant Id,
        // NOT the in-memory Guid the fetcher generated
        Assert.Equal(dbVariant.Id, storedPrice.VariantId);
    }

    [Fact]
    public async Task PersistAsync_CS2ShStyleResult_MultipleVariantsAndPrices_AllPricesLand()
    {
        var item = BuildItem(slug: "karambit-doppler");
        var baseVariant = BuildVariant(item, "karambit-doppler-factory-new");
        var phase2Variant = BuildVariant(item, "karambit-doppler-factory-new-p2");
        var rubyVariant = BuildVariant(item, "karambit-doppler-factory-new-ruby");

        var basePrice = BuildPrice(baseVariant, Sources.CS2ShBuff);
        var phase2Price = BuildPrice(phase2Variant, Sources.CS2ShBuff);
        var rubyPrice = BuildPrice(rubyVariant, Sources.CS2ShBuff);

        var items = new List<SkinItem> { item };
        var variants = new List<SkinVariant> { baseVariant, phase2Variant, rubyVariant };
        var prices = new List<SkinPrice> { basePrice, phase2Price, rubyPrice };

        var result = BuildCs2ShResult(items, variants, prices);

        await _persister.PersistAsync(result, CancellationToken.None);

        var storedPrices = await _db.CurrentPrices.OrderBy(p => p.Slug).ToListAsync();
        Assert.Equal(3, storedPrices.Count);
        Assert.Contains(storedPrices, p => p.Slug == "karambit-doppler-factory-new");
        Assert.Contains(storedPrices, p => p.Slug == "karambit-doppler-factory-new-p2");
        Assert.Contains(storedPrices, p => p.Slug == "karambit-doppler-factory-new-ruby");
    }

    [Fact]
    public async Task PersistAsync_CS2ShStyleResult_MultipleMarketplaces_AllPricesLand()
    {
        var item = BuildItem(slug: "ak-47-redline");
        var variant = BuildVariant(item, "ak-47-redline-field-tested");

        var buffPrice = BuildPrice(variant, Sources.CS2ShBuff);
        var csfloatPrice = BuildPrice(variant, Sources.CS2ShCsFloat);
        var steamPrice = BuildPrice(variant, Sources.CS2ShSteam);

        var items = new List<SkinItem> { item };
        var variants = new List<SkinVariant> { variant };
        var prices = new List<SkinPrice> { buffPrice, csfloatPrice, steamPrice };

        var result = BuildCs2ShResult(items, variants, prices);

        await _persister.PersistAsync(result, CancellationToken.None);

        var storedPrices = await _db.CurrentPrices.OrderBy(p => p.Source).ToListAsync();
        Assert.Equal(3, storedPrices.Count);
        Assert.Contains(storedPrices, p => p.Source == Sources.CS2ShBuff);
        Assert.Contains(storedPrices, p => p.Source == Sources.CS2ShCsFloat);
        Assert.Contains(storedPrices, p => p.Source == Sources.CS2ShSteam);
    }

    #endregion

    #region Prices-Only fetchers (Pricempire / SteamAnalyst / Kaggle style)

    // Prices-only fetchers set VariantId = Guid.Empty and rely entirely on slug resolution.
    // Variants must already be seeded in the DB before the price fetch runs.
    [Fact]
    public async Task PersistAsync_PricesOnlyResult_ResolvesVariantViaSlug()
    {
        // Seed items and variants via a prior item+variant fetch (e.g. cs2.sh or ByMykel)
        var item = BuildItem(slug: "ak-47-redline");
        var variant = BuildVariant(item, "ak-47-redline-field-tested");
        var seedResult = BuildCs2ShResult(
            new List<SkinItem> { item },
            new List<SkinVariant> { variant },
            []);
        await _persister.PersistAsync(seedResult, CancellationToken.None);

        // Now run a prices-only fetch — VariantId is Guid.Empty, resolved via slug
        var priceOnly = new SkinPrice
        {
            VariantId = Guid.Empty,
            GameId = GameIds.CounterStrike,
            Slug = "ak-47-redline-field-tested",
            Source = Sources.Pricempire,
            PriceType = "avg_7d",
            Price = 30.00m,
            Currency = "USD",
            Volume = null,
            RecordedAt = DateTime.UtcNow,
        };

        var pricesOnlyResult = FetchResult.Success(
            gameId: GameIds.CounterStrike,
            source: Sources.Pricempire,
            items: [],
            variants: [],
            prices: new List<SkinPrice> { priceOnly });

        await _persister.PersistAsync(pricesOnlyResult, CancellationToken.None);

        var storedPrice = await _db.CurrentPrices
            .SingleAsync(p => p.Source == Sources.Pricempire);

        var dbVariant = await _db.Variants.SingleAsync();
        Assert.Equal(dbVariant.Id, storedPrice.VariantId);
        Assert.Equal(30.00m, storedPrice.Price);
    }

    [Fact]
    public async Task PersistAsync_PricesOnlyResult_NoVariantsSeeded_SkipsGracefully()
    {
        var priceOnly = new SkinPrice
        {
            VariantId = Guid.Empty,
            GameId = GameIds.CounterStrike,
            Slug = "ak-47-redline-field-tested",
            Source = Sources.Pricempire,
            PriceType = "avg_7d",
            Price = 30.00m,
            Currency = "USD",
            Volume = null,
            RecordedAt = DateTime.UtcNow,
        };

        var result = FetchResult.Success(
            gameId: GameIds.CounterStrike,
            source: Sources.Pricempire,
            items: [],
            variants: [],
            prices: new List<SkinPrice> { priceOnly });

        // Must not throw — slug map is empty, persister should log warning and return early
        await _persister.PersistAsync(result, CancellationToken.None);

        var count = await _db.CurrentPrices.CountAsync();
        Assert.Equal(0, count);
    }

    #endregion

    #region Idempotency

    [Fact]
    public async Task PersistAsync_SameResultTwice_DoesNotDuplicatePrices()
    {
        var item = BuildItem(slug: "ak-47-redline");
        var variant = BuildVariant(item, "ak-47-redline-field-tested");
        var price = BuildPrice(variant);

        var items = new List<SkinItem> { item };
        var variants = new List<SkinVariant> { variant };
        var prices = new List<SkinPrice> { price };

        var result = BuildCs2ShResult(items, variants, prices);

        await _persister.PersistAsync(result, CancellationToken.None);
        await _persister.PersistAsync(result, CancellationToken.None);

        // current_prices is one row per (variant_id, source, price_type)
        var currentCount = await _db.CurrentPrices.CountAsync();
        Assert.Equal(1, currentCount);
    }

    #endregion
}