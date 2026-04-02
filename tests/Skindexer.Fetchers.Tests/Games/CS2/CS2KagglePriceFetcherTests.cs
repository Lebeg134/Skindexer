using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Skindexer.Contracts.Constants;
using Skindexer.Fetchers.Games.CS2.Fetchers;
using Skindexer.Fetchers.Options;

namespace Skindexer.Fetchers.Tests.Games.CS2;

public class CS2KagglePriceFetcherTests
{
    #region Test Data Builders

    private static CS2KagglePriceFetcher CreateFetcher(string dataPath)
    {
        var options = Substitute.For<IOptions<KaggleFetcherOptions>>();
        options.Value.Returns(new KaggleFetcherOptions { CS2DataPath = dataPath });

        return new CS2KagglePriceFetcher(options, NullLogger<CS2KagglePriceFetcher>.Instance);
    }

    private static string GetTestDataPath() =>
        Path.Combine(AppContext.BaseDirectory, "TestData", "Kaggle");

    private static string GetMissingPath() =>
        Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

    #endregion

    #region Construction

    [Fact]
    public void Constructor_WithValidOptions_CreatesSuccessfully()
    {
        var options = Substitute.For<IOptions<KaggleFetcherOptions>>();
        options.Value.Returns(new KaggleFetcherOptions { CS2DataPath = GetTestDataPath() });

        var fetcher = new CS2KagglePriceFetcher(options, NullLogger<CS2KagglePriceFetcher>.Instance);

        Assert.NotNull(fetcher);
        Assert.Equal("cs2-kaggle-steam", fetcher.FetcherId);
        Assert.Equal([".csv"], fetcher.SupportedExtensions);
    }

    #endregion

    #region Guard Clauses

    [Fact]
    public async Task FetchAsync_MissingItemsDirectory_ReturnsFailure()
    {
        var fetcher = CreateFetcher(GetMissingPath());

        var result = await fetcher.FetchAsync();

        Assert.False(result.IsSuccess);
        Assert.NotNull(result.ErrorMessage);
    }

    #endregion

    #region Result Shape

    [Fact]
    public async Task FetchAsync_ValidDataPath_ReturnsCorrectGameIdAndSource()
    {
        var fetcher = CreateFetcher(GetTestDataPath());

        var result = await fetcher.FetchAsync();

        Assert.Equal("cs2", result.GameId);
        Assert.Equal(Sources.KaggleSteam, result.Source);
    }

    [Fact]
    public async Task FetchAsync_ValidDataPath_ReturnsNoItems()
    {
        var fetcher = CreateFetcher(GetTestDataPath());

        var result = await fetcher.FetchAsync();

        // Kaggle fetcher is prices-only — item seeding is handled by ByMykel fetcher
        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task FetchAsync_ValidDataPath_ReturnsPrices()
    {
        var fetcher = CreateFetcher(GetTestDataPath());

        var result = await fetcher.FetchAsync();

        Assert.NotEmpty(result.Prices);
    }

    [Fact]
    public async Task FetchAsync_ValidDataPath_AllPricesHaveGuidEmptyItemId()
    {
        var fetcher = CreateFetcher(GetTestDataPath());

        var result = await fetcher.FetchAsync();

        Assert.All(result.Prices, p => Assert.Equal(Guid.Empty, p.VariantId));
    }

    [Fact]
    public async Task FetchAsync_ValidDataPath_AllPricesHaveCorrectSource()
    {
        var fetcher = CreateFetcher(GetTestDataPath());

        var result = await fetcher.FetchAsync();

        Assert.All(result.Prices, p => Assert.Equal(Sources.KaggleSteam, p.Source));
    }

    [Fact]
    public async Task FetchAsync_ValidDataPath_AllPricesHaveCorrectPriceType()
    {
        var fetcher = CreateFetcher(GetTestDataPath());

        var result = await fetcher.FetchAsync();

        Assert.All(result.Prices, p => Assert.Equal(PriceTypes.MedianDaily, p.PriceType));
    }

    [Fact]
    public async Task FetchAsync_ValidDataPath_AllPricesHaveUsdCurrency()
    {
        var fetcher = CreateFetcher(GetTestDataPath());

        var result = await fetcher.FetchAsync();

        Assert.All(result.Prices, p => Assert.Equal("USD", p.Currency));
    }

    [Fact]
    public async Task FetchAsync_ValidDataPath_AllPricesHavePositivePrice()
    {
        var fetcher = CreateFetcher(GetTestDataPath());

        var result = await fetcher.FetchAsync();

        Assert.All(result.Prices, p => Assert.True(p.Price > 0));
    }

    [Fact]
    public async Task FetchAsync_ValidDataPath_AllPricesHaveNonEmptySlug()
    {
        var fetcher = CreateFetcher(GetTestDataPath());

        var result = await fetcher.FetchAsync();

        Assert.All(result.Prices, p => Assert.False(string.IsNullOrWhiteSpace(p.Slug)));
    }

    #endregion

    #region Slug Generation

    [Fact]
    public async Task FetchAsync_NormalSkin_ProducesCorrectSlug()
    {
        var fetcher = CreateFetcher(GetTestDataPath());
        var expectedSlug = "dual-berettas-royal-consorts-well-worn";

        var result = await fetcher.FetchAsync();

        Assert.Contains(result.Prices, p => p.Slug == expectedSlug);
    }

    [Fact]
    public async Task FetchAsync_StatTrakSkin_ProducesCorrectSlug()
    {
        var fetcher = CreateFetcher(GetTestDataPath());
        var expectedSlug = "awp-asiimov-stattrak-field-tested";

        var result = await fetcher.FetchAsync();

        Assert.Contains(result.Prices, p => p.Slug == expectedSlug);
    }

    [Fact]
    public async Task FetchAsync_StatTrakSkin_DoesNotProduceNonStatTrakSlug()
    {
        var fetcher = CreateFetcher(GetTestDataPath());
        var nonStatTrakSlug = "awp-asiimov-field-tested";

        var result = await fetcher.FetchAsync();

        Assert.DoesNotContain(result.Prices, p => p.Slug == nonStatTrakSlug);
    }

    [Fact]
    public async Task FetchAsync_KnifeWithWear_ProducesCorrectSlug()
    {
        var fetcher = CreateFetcher(GetTestDataPath());
        var expectedSlug = "bayonet-boreal-forest-factory-new";

        var result = await fetcher.FetchAsync();

        Assert.Contains(result.Prices, p => p.Slug == expectedSlug);
    }

    [Fact]
    public async Task FetchAsync_ButterflyKnifeWithWear_ProducesCorrectSlug()
    {
        var fetcher = CreateFetcher(GetTestDataPath());
        var expectedSlug = "butterfly-knife-damascus-steel-factory-new";

        var result = await fetcher.FetchAsync();

        Assert.Contains(result.Prices, p => p.Slug == expectedSlug);
    }

    #endregion

    #region Skipped Items

    [Fact]
    public async Task FetchAsync_StickerFile_IsSkippedWithWarning()
    {
        var fetcher = CreateFetcher(GetTestDataPath());

        var result = await fetcher.FetchAsync();

        var hasWarning = result.Warnings.Any(w =>
            w.Contains("m0NESY", StringComparison.OrdinalIgnoreCase) ||
            w.Contains("Sticker", StringComparison.OrdinalIgnoreCase));

        Assert.True(hasWarning);
        Assert.DoesNotContain(result.Prices, p => p.Slug.Contains("m0nesy"));
    }

    [Fact]
    public async Task FetchAsync_GraffitiFile_IsSkippedWithWarning()
    {
        var fetcher = CreateFetcher(GetTestDataPath());

        var result = await fetcher.FetchAsync();

        var hasWarning = result.Warnings.Any(w =>
            w.Contains("Graffiti", StringComparison.OrdinalIgnoreCase) ||
            w.Contains("PGL", StringComparison.OrdinalIgnoreCase));

        Assert.True(hasWarning);
        Assert.DoesNotContain(result.Prices, p => p.Slug.Contains("pgl"));
    }

    [Fact]
    public async Task FetchAsync_AgentFile_IsSkippedWithWarning()
    {
        var fetcher = CreateFetcher(GetTestDataPath());

        var result = await fetcher.FetchAsync();

        var hasWarning = result.Warnings.Any(w =>
            w.Contains("Blueberries", StringComparison.OrdinalIgnoreCase) ||
            w.Contains("NSWC", StringComparison.OrdinalIgnoreCase));

        Assert.True(hasWarning);
    }

    [Fact]
    public async Task FetchAsync_SkippedItems_ProduceNoGraffitiOrStickerSlugs()
    {
        var fetcher = CreateFetcher(GetTestDataPath());

        var result = await fetcher.FetchAsync();

        Assert.DoesNotContain(result.Prices, p => p.Slug.Contains("graffiti"));
        Assert.DoesNotContain(result.Prices, p => p.Slug.Contains("sticker"));
    }

    #endregion

    #region Recorded At

    [Fact]
    public async Task FetchAsync_StatTrakAwp_RecordedAtMatchesUnixTimestamp()
    {
        var fetcher = CreateFetcher(GetTestDataPath());
        var expectedTimestamp = DateTimeOffset.FromUnixTimeSeconds(1392944400).UtcDateTime;

        var result = await fetcher.FetchAsync();

        var awpPrices = result.Prices
            .Where(p => p.Slug == "awp-asiimov-stattrak-field-tested")
            .ToList();

        Assert.NotEmpty(awpPrices);
        Assert.Contains(awpPrices, p => p.RecordedAt == expectedTimestamp);
    }

    [Fact]
    public async Task FetchAsync_AllPrices_RecordedAtIsUtc()
    {
        var fetcher = CreateFetcher(GetTestDataPath());

        var result = await fetcher.FetchAsync();

        Assert.All(result.Prices, p => Assert.Equal(DateTimeKind.Utc, p.RecordedAt.Kind));
    }

    #endregion
}