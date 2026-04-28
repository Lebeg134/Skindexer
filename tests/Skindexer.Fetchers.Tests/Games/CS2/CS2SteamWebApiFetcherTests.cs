using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Skindexer.Contracts.Constants;
using Skindexer.Fetchers.Games.CS2.Fetchers.SteamWebApiFetcher;

namespace Skindexer.Fetchers.Tests.Games.CS2;

public class CS2SteamWebApiFetcherTests
{
    #region Test Data Builders

    private static CS2SteamWebApiFetcher BuildFetcher(HttpMessageHandler handler)
    {
        var factory = new FakeHttpClientFactory(nameof(CS2SteamWebApiFetcher), handler);
        var config  = BuildConfig();
        return new CS2SteamWebApiFetcher(factory, config, NullLogger<CS2SteamWebApiFetcher>.Instance);
    }

    private static IConfiguration BuildConfig() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["SteamWebApi:ApiKey"] = "test-api-key"
            })
            .Build();

    private static string SingleItemJson(
        string marketHashName  = "AK-47 | Redline (Field-Tested)",
        string priceLatest     = "15.50",
        string priceLatestSell = "15.00",
        string buyOrderPrice   = "14.50",
        string priceReal       = "13.00",
        int    sold24h         = 42,
        string image           = "https://example.com/ak47.png",
        string rarity          = "Classified") => $$"""
        [
            {
                "markethashname":  "{{marketHashName}}",
                "pricelatest":     {{priceLatest}},
                "pricelatestsell": {{priceLatestSell}},
                "buyorderprice":   {{buyOrderPrice}},
                "pricereal":       {{priceReal}},
                "sold24h":         {{sold24h}},
                "image":           "{{image}}",
                "rarity":          "{{rarity}}"
            }
        ]
        """;

    private static string MultiItemJson() => """
        [
            {
                "markethashname":  "AK-47 | Redline (Field-Tested)",
                "pricelatest":     15.50,
                "pricelatestsell": 15.00,
                "buyorderprice":   14.50,
                "pricereal":       13.00,
                "sold24h":         42,
                "image":           "https://example.com/ak47.png",
                "rarity":          "Classified"
            },
            {
                "markethashname":  "AK-47 | Redline (Factory New)",
                "pricelatest":     45.00,
                "pricelatestsell": 44.00,
                "buyorderprice":   43.00,
                "pricereal":       40.00,
                "sold24h":         10,
                "image":           "https://example.com/ak47.png",
                "rarity":          "Classified"
            },
            {
                "markethashname":  "StatTrak™ AK-47 | Redline (Field-Tested)",
                "pricelatest":     35.00,
                "pricelatestsell": 34.00,
                "buyorderprice":   33.00,
                "pricereal":       30.00,
                "sold24h":         8,
                "image":           "https://example.com/ak47.png",
                "rarity":          "Classified"
            }
        ]
        """;

    private sealed class FakeHttpMessageHandler(HttpStatusCode statusCode, string json)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
            });
    }

    private sealed class FakeHttpClientFactory(string expectedClientName, HttpMessageHandler handler)
        : IHttpClientFactory
    {
        public HttpClient CreateClient(string name)
        {
            Assert.Equal(expectedClientName, name);
            return new HttpClient(handler);
        }
    }

    #endregion

    #region Construction

    [Fact]
    public void Constructor_MissingApiKey_Throws()
    {
        var emptyConfig = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        var factory = new FakeHttpClientFactory(
            nameof(CS2SteamWebApiFetcher),
            new FakeHttpMessageHandler(HttpStatusCode.OK, "[]"));

        var ex = Assert.Throws<InvalidOperationException>(() =>
            new CS2SteamWebApiFetcher(factory, emptyConfig, NullLogger<CS2SteamWebApiFetcher>.Instance));

        Assert.Contains("SteamWebApi:ApiKey", ex.Message);
    }

    #endregion

    #region Guard Clauses

    [Fact]
    public async Task FetchAsync_ApiReturnsNonSuccess_ReturnsFailure()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.Unauthorized, "");
        var fetcher = BuildFetcher(handler);

        var result = await fetcher.FetchAsync();

        Assert.False(result.IsSuccess);
        Assert.NotNull(result.ErrorMessage);
    }

    [Fact]
    public async Task FetchAsync_EmptyArray_ReturnsSuccessWithNoData()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, "[]");
        var fetcher = BuildFetcher(handler);

        var result = await fetcher.FetchAsync();

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Items);
        Assert.Empty(result.Variants);
        Assert.Empty(result.Prices);
    }

    #endregion

    #region Parsing

    [Fact]
    public async Task FetchAsync_ValidItem_ReturnsSingleItemAndVariant()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, SingleItemJson());
        var fetcher = BuildFetcher(handler);

        var result = await fetcher.FetchAsync();

        Assert.True(result.IsSuccess);
        Assert.Single(result.Items);
        Assert.Single(result.Variants);
    }

    [Fact]
    public async Task FetchAsync_ValidItem_ItemHasCorrectSlugAndName()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, SingleItemJson());
        var fetcher = BuildFetcher(handler);

        var result = await fetcher.FetchAsync();

        var item = result.Items[0];
        Assert.Equal("ak-47-redline", item.Slug);
        Assert.Equal("AK-47 | Redline", item.Name);
    }

    [Fact]
    public async Task FetchAsync_ValidItem_VariantHasCorrectSlug()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, SingleItemJson());
        var fetcher = BuildFetcher(handler);

        var result = await fetcher.FetchAsync();

        var variant = result.Variants[0];
        Assert.Equal("ak-47-redline-field-tested", variant.Slug);
    }

    [Fact]
    public async Task FetchAsync_StatTrakItem_VariantSlugContainsStatTrak()
    {
        var json    = SingleItemJson(marketHashName: "StatTrak™ AK-47 | Redline (Minimal Wear)");
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, json);
        var fetcher = BuildFetcher(handler);

        var result = await fetcher.FetchAsync();

        var variant = result.Variants[0];
        Assert.Equal("ak-47-redline-stattrak-minimal-wear", variant.Slug);
    }

    [Fact]
    public async Task FetchAsync_SouvenirItem_VariantSlugContainsSouvenir()
    {
        var json    = SingleItemJson(marketHashName: "Souvenir AK-47 | Redline (Factory New)");
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, json);
        var fetcher = BuildFetcher(handler);

        var result = await fetcher.FetchAsync();

        var variant = result.Variants[0];
        Assert.Equal("ak-47-redline-souvenir-factory-new", variant.Slug);
    }

    [Fact]
    public async Task FetchAsync_NonWeaponItem_IsSkipped()
    {
        var json    = SingleItemJson(marketHashName: "Chroma 2 Case");
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, json);
        var fetcher = BuildFetcher(handler);

        var result = await fetcher.FetchAsync();

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Items);
        Assert.Empty(result.Variants);
    }

    [Fact]
    public async Task FetchAsync_DopplerWithPhase_IsIngested()
    {
        // Dopplers with phase parse successfully and are ingested with a phase-aware slug.
        // Paint index mapping is not yet implemented — tracked on GitHub.
        // e.g. "★ Bayonet | Doppler Phase 2 (Factory New)" → "bayonet-doppler-phase-2-factory-new"
        var json    = SingleItemJson(marketHashName: "★ Bayonet | Doppler Phase 2 (Factory New)");
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, json);
        var fetcher = BuildFetcher(handler);

        var result = await fetcher.FetchAsync();

        Assert.True(result.IsSuccess);
        Assert.Single(result.Items);
        Assert.Equal("bayonet-doppler-phase-2", result.Items[0].Slug);
        Assert.Equal("bayonet-doppler-phase-2-factory-new", result.Variants[0].Slug);
    }

    #endregion

    #region Item Deduplication

    [Fact]
    public async Task FetchAsync_MultipleVariantsSameBaseItem_ProducesOneItem()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, MultiItemJson());
        var fetcher = BuildFetcher(handler);

        var result = await fetcher.FetchAsync();

        // AK-47 | Redline (FT), (FN), StatTrak (FT) → one base item, three variants
        Assert.Single(result.Items);
        Assert.Equal(3, result.Variants.Count);
    }

    [Fact]
    public async Task FetchAsync_MultipleVariantsSameBaseItem_AllVariantsReferenceCorrectItemId()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, MultiItemJson());
        var fetcher = BuildFetcher(handler);

        var result = await fetcher.FetchAsync();

        var itemId = result.Items[0].Id;
        Assert.All(result.Variants, v => Assert.Equal(itemId, v.ItemId));
    }

    #endregion

    #region Prices

    [Fact]
    public async Task FetchAsync_ValidItem_ProducesFourPriceRows()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, SingleItemJson());
        var fetcher = BuildFetcher(handler);

        var result = await fetcher.FetchAsync();

        Assert.Equal(4, result.Prices.Count);
    }

    [Fact]
    public async Task FetchAsync_ValidItem_AllExpectedPriceTypesPresent()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, SingleItemJson());
        var fetcher = BuildFetcher(handler);

        var result  = await fetcher.FetchAsync();
        var types   = result.Prices.Select(p => p.PriceType).ToHashSet();

        Assert.Contains(PriceTypes.LowestListing, types);
        Assert.Contains(PriceTypes.LastSold,       types);
        Assert.Contains(PriceTypes.BuyOrder,       types);
        Assert.Contains(PriceTypes.LowestMarket,   types);
    }

    [Fact]
    public async Task FetchAsync_ValidItem_LastSoldHasSold24hAsVolume()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, SingleItemJson(sold24h: 42));
        var fetcher = BuildFetcher(handler);

        var result    = await fetcher.FetchAsync();
        var lastSold  = result.Prices.Single(p => p.PriceType == PriceTypes.LastSold);

        Assert.Equal(42, lastSold.Volume);
    }

    [Fact]
    public async Task FetchAsync_ValidItem_NonLastSoldPricesHaveNullVolume()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, SingleItemJson());
        var fetcher = BuildFetcher(handler);

        var result = await fetcher.FetchAsync();
        var others = result.Prices.Where(p => p.PriceType != PriceTypes.LastSold);

        Assert.All(others, p => Assert.Null(p.Volume));
    }

    [Fact]
    public async Task FetchAsync_ValidItem_PricesHaveCorrectValues()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, SingleItemJson(
            priceLatest:     "15.50",
            priceLatestSell: "15.00",
            buyOrderPrice:   "14.50",
            priceReal:       "13.00"));
        var fetcher = BuildFetcher(handler);

        var result = await fetcher.FetchAsync();

        Assert.Equal(15.50m, result.Prices.Single(p => p.PriceType == PriceTypes.LowestListing).Price);
        Assert.Equal(15.00m, result.Prices.Single(p => p.PriceType == PriceTypes.LastSold).Price);
        Assert.Equal(14.50m, result.Prices.Single(p => p.PriceType == PriceTypes.BuyOrder).Price);
        Assert.Equal(13.00m, result.Prices.Single(p => p.PriceType == PriceTypes.LowestMarket).Price);
    }

    [Fact]
    public async Task FetchAsync_ZeroOrNullPrice_PriceRowOmitted()
    {
        var json = SingleItemJson(priceReal: "0");
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, json);
        var fetcher = BuildFetcher(handler);

        var result = await fetcher.FetchAsync();

        Assert.DoesNotContain(result.Prices, p => p.PriceType == PriceTypes.LowestMarket);
        Assert.Equal(3, result.Prices.Count);
    }

    [Fact]
    public async Task FetchAsync_ValidItem_AllPricesSourcedFromSteamWebApi()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, SingleItemJson());
        var fetcher = BuildFetcher(handler);

        var result = await fetcher.FetchAsync();

        Assert.All(result.Prices, p => Assert.Equal(Sources.SteamWebApi, p.Source));
    }

    [Fact]
    public async Task FetchAsync_ValidItem_AllPricesCurrencyUsd()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, SingleItemJson());
        var fetcher = BuildFetcher(handler);

        var result = await fetcher.FetchAsync();

        Assert.All(result.Prices, p => Assert.Equal("USD", p.Currency));
    }

    #endregion

    #region Metadata

    [Fact]
    public async Task FetchAsync_ValidItem_VariantMetadataContainsWearAndFlags()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, SingleItemJson());
        var fetcher = BuildFetcher(handler);

        var result  = await fetcher.FetchAsync();
        var variant = result.Variants[0];

        Assert.Equal("Field-Tested", variant.Metadata["wear"]);
        Assert.Equal(false,          variant.Metadata["stattrak"]);
        Assert.Equal(false,          variant.Metadata["souvenir"]);
        Assert.Equal("Classified",   variant.Metadata["rarity"]);
    }

    [Fact]
    public async Task FetchAsync_ValidItem_ItemImageUrlSet()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK,
            SingleItemJson(image: "https://example.com/ak47.png"));
        var fetcher = BuildFetcher(handler);

        var result = await fetcher.FetchAsync();

        Assert.Equal("https://example.com/ak47.png", result.Items[0].ImageUrl);
    }

    [Fact]
    public async Task FetchAsync_ValidItem_ItemIsMarketableAndTradeable()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, SingleItemJson());
        var fetcher = BuildFetcher(handler);

        var result = await fetcher.FetchAsync();

        Assert.True(result.Items[0].IsMarketable);
        Assert.True(result.Items[0].IsTradeable);
    }

    #endregion

    #region File-Based

    [Fact]
    public async Task FetchAsync_RealResponseFile_ParsesWithoutErrors()
    {
        var json    = await File.ReadAllTextAsync("TestData/SteamWebApi/prices_response.json");
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, json);
        var fetcher = BuildFetcher(handler);

        var result = await fetcher.FetchAsync();

        Assert.True(result.IsSuccess);
        Assert.NotEmpty(result.Items);
        Assert.NotEmpty(result.Variants);
        Assert.NotEmpty(result.Prices);
        Assert.Equal(115, result.Items.Count);
        Assert.Equal(558, result.Prices.Count);
    }

    #endregion
}