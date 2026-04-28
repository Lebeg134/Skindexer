using System.IO.Compression;
using System.Net;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Skindexer.Contracts.Constants;
using Skindexer.Fetchers.Games.CS2.Fetchers.CS2Sh;

namespace Skindexer.Fetchers.Tests.Games.CS2;

public class CS2CS2ShFetcherTests
{
    #region Test Data Builders

    private static CS2CS2ShFetcher BuildFetcher(HttpMessageHandler handler)
    {
        var factory = new FakeHttpClientFactory(nameof(CS2CS2ShFetcher), handler);
        var config = BuildConfig();
        return new CS2CS2ShFetcher(factory, config, NullLogger<CS2CS2ShFetcher>.Instance);
    }

    private static IConfiguration BuildConfig() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["CS2Sh:ApiKey"] = "test-api-key"
            })
            .Build();

    // cs2.sh always returns gzip — tests must mirror this
    private static byte[] GzipJson(string json)
    {
        using var ms = new MemoryStream();
        using var gzip = new GZipStream(ms, CompressionMode.Compress);
        using var writer = new StreamWriter(gzip, Encoding.UTF8);
        writer.Write(json);
        writer.Flush();
        gzip.Flush();
        return ms.ToArray();
    }

    private static string SingleItemJson(
        string marketHashName = "AK-47 | Redline (Field-Tested)",
        decimal buffAsk = 34.30m,
        decimal buffBid = 32.86m,
        int buffAskVolume = 10444,
        int buffBidVolume = 209,
        decimal steamAsk = 49.64m,
        int steamAskVolume = 874) => $$"""
                                       {
                                           "items": {
                                               "{{marketHashName}}": {
                                                   "market_hash_name": "{{marketHashName}}",
                                                   "buff": {
                                                       "ask": {{buffAsk}},
                                                       "ask_volume": {{buffAskVolume}},
                                                       "bid": {{buffBid}},
                                                       "bid_volume": {{buffBidVolume}}
                                                   },
                                                   "steam": {
                                                       "ask": {{steamAsk}},
                                                       "ask_volume": {{steamAskVolume}}
                                                   }
                                               }
                                           }
                                       }
                                       """;

    private static string DopplerItemJson() => """
                                               {
                                                   "items": {
                                                       "★ Karambit | Doppler (Factory New)": {
                                                           "market_hash_name": "★ Karambit | Doppler (Factory New)",
                                                           "buff": {
                                                               "ask": 1393.61,
                                                               "ask_volume": 1654,
                                                               "bid": 1361.91,
                                                               "bid_volume": 68
                                                           },
                                                           "variants": {
                                                               "Phase 2": {
                                                                   "display_name": "Phase 2",
                                                                   "version": "p2",
                                                                   "buff": {
                                                                       "ask": 2189.14,
                                                                       "ask_volume": null,
                                                                       "bid": 2055.11,
                                                                       "bid_volume": null
                                                                   }
                                                               },
                                                               "Ruby": {
                                                                   "display_name": "Ruby",
                                                                   "version": "ruby",
                                                                   "buff": {
                                                                       "ask": 4500.00,
                                                                       "ask_volume": null,
                                                                       "bid": 4200.00,
                                                                       "bid_volume": null
                                                                   }
                                                               }
                                                           }
                                                       }
                                                   }
                                               }
                                               """;

    private static string MultiVariantJson() => """
                                                {
                                                    "items": {
                                                        "AK-47 | Redline (Field-Tested)": {
                                                            "market_hash_name": "AK-47 | Redline (Field-Tested)",
                                                            "buff": { "ask": 34.30, "ask_volume": 10444, "bid": 32.86, "bid_volume": 209 }
                                                        },
                                                        "AK-47 | Redline (Factory New)": {
                                                            "market_hash_name": "AK-47 | Redline (Factory New)",
                                                            "buff": { "ask": 80.00, "ask_volume": 500, "bid": 78.00, "bid_volume": 100 }
                                                        },
                                                        "StatTrak™ AK-47 | Redline (Field-Tested)": {
                                                            "market_hash_name": "StatTrak™ AK-47 | Redline (Field-Tested)",
                                                            "buff": { "ask": 55.00, "ask_volume": 200, "bid": 53.00, "bid_volume": 50 }
                                                        }
                                                    }
                                                }
                                                """;

    private static string NullBidJson() => """
                                           {
                                               "items": {
                                                   "AK-47 | Redline (Field-Tested)": {
                                                       "market_hash_name": "AK-47 | Redline (Field-Tested)",
                                                       "buff": { "ask": 34.30, "ask_volume": 10444, "bid": null, "bid_volume": null },
                                                       "csfloat": { "ask": 33.29, "ask_volume": 2510 }
                                                   }
                                               }
                                           }
                                           """;

    private sealed class FakeHttpMessageHandler(HttpStatusCode statusCode, byte[] body)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(statusCode)
            {
                Content = new ByteArrayContent(body)
            };
            response.Content.Headers.ContentEncoding.Add("gzip");
            response.Content.Headers.ContentType =
                new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
            return Task.FromResult(response);
        }
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

    private static CS2CS2ShFetcher BuildFetcherWithJson(string json) =>
        BuildFetcher(new FakeHttpMessageHandler(HttpStatusCode.OK, GzipJson(json)));

    #endregion

    #region Construction

    [Fact]
    public void Constructor_MissingApiKey_Throws()
    {
        var emptyConfig = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        var factory = new FakeHttpClientFactory(
            nameof(CS2CS2ShFetcher),
            new FakeHttpMessageHandler(HttpStatusCode.OK, GzipJson("{}")));

        var ex = Assert.Throws<InvalidOperationException>(() =>
            new CS2CS2ShFetcher(factory, emptyConfig, NullLogger<CS2CS2ShFetcher>.Instance));

        Assert.Contains("CS2Sh:ApiKey", ex.Message);
    }

    #endregion

    #region Guard Clauses

    [Fact]
    public async Task FetchAsync_ApiReturnsNonSuccess_ReturnsFailure()
    {
        var fetcher = BuildFetcher(
            new FakeHttpMessageHandler(HttpStatusCode.Unauthorized, GzipJson("{}")));

        var result = await fetcher.FetchAsync();

        Assert.False(result.IsSuccess);
        Assert.NotNull(result.ErrorMessage);
    }

    [Fact]
    public async Task FetchAsync_EmptyItemsDictionary_ReturnsSuccessWithNoData()
    {
        var fetcher = BuildFetcherWithJson("""{ "items": {} }""");

        var result = await fetcher.FetchAsync();

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Items);
        Assert.Empty(result.Variants);
        Assert.Empty(result.Prices);
    }

    [Fact]
    public async Task FetchAsync_NonWeaponItem_IsSkipped()
    {
        var fetcher = BuildFetcherWithJson(SingleItemJson(marketHashName: "Chroma 2 Case"));

        var result = await fetcher.FetchAsync();

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Items);
        Assert.Empty(result.Variants);
    }

    #endregion

    #region Parsing

    [Fact]
    public async Task FetchAsync_ValidItem_ReturnsSingleItemAndVariant()
    {
        var fetcher = BuildFetcherWithJson(SingleItemJson());

        var result = await fetcher.FetchAsync();

        Assert.True(result.IsSuccess);
        Assert.Single(result.Items);
        Assert.Single(result.Variants);
    }

    [Fact]
    public async Task FetchAsync_ValidItem_ItemHasCorrectSlugAndName()
    {
        var fetcher = BuildFetcherWithJson(SingleItemJson());

        var result = await fetcher.FetchAsync();

        var item = result.Items[0];
        Assert.Equal("ak-47-redline", item.Slug);
        Assert.Equal("AK-47 | Redline", item.Name);
    }

    [Fact]
    public async Task FetchAsync_ValidItem_VariantHasCorrectSlug()
    {
        var fetcher = BuildFetcherWithJson(SingleItemJson());

        var result = await fetcher.FetchAsync();

        Assert.Equal("ak-47-redline-field-tested", result.Variants[0].Slug);
    }

    [Fact]
    public async Task FetchAsync_StatTrakItem_VariantSlugContainsStatTrak()
    {
        var fetcher = BuildFetcherWithJson(
            SingleItemJson(marketHashName: "StatTrak™ AK-47 | Redline (Minimal Wear)"));

        var result = await fetcher.FetchAsync();

        Assert.Equal("ak-47-redline-stattrak-minimal-wear", result.Variants[0].Slug);
    }

    #endregion

    #region Item Deduplication

    [Fact]
    public async Task FetchAsync_MultipleVariantsSameBaseItem_ProducesOneItem()
    {
        var fetcher = BuildFetcherWithJson(MultiVariantJson());

        var result = await fetcher.FetchAsync();

        Assert.Single(result.Items);
        Assert.Equal(3, result.Variants.Count);
    }

    [Fact]
    public async Task FetchAsync_MultipleVariantsSameBaseItem_AllVariantsReferenceCorrectItemId()
    {
        var fetcher = BuildFetcherWithJson(MultiVariantJson());

        var result = await fetcher.FetchAsync();

        var itemId = result.Items[0].Id;
        Assert.All(result.Variants, v => Assert.Equal(itemId, v.ItemId));
    }

    #endregion

    #region Doppler Variants

    [Fact]
    public async Task FetchAsync_DopplerItem_ProducesBaseVariantPlusPhaseVariants()
    {
        var fetcher = BuildFetcherWithJson(DopplerItemJson());

        var result = await fetcher.FetchAsync();

        // 1 base variant + 2 phases (Phase 2, Ruby)
        Assert.Single(result.Items);
        Assert.Equal(3, result.Variants.Count);
    }

    [Fact]
    public async Task FetchAsync_DopplerItem_BaseVariantHasCorrectSlug()
    {
        var fetcher = BuildFetcherWithJson(DopplerItemJson());

        var result = await fetcher.FetchAsync();

        Assert.Contains(result.Variants, v => v.Slug == "karambit-doppler-factory-new");
    }

    [Fact]
    public async Task FetchAsync_DopplerItem_PhaseVariantsHaveVersionSuffix()
    {
        var fetcher = BuildFetcherWithJson(DopplerItemJson());

        var result = await fetcher.FetchAsync();

        Assert.Contains(result.Variants, v => v.Slug == "karambit-doppler-factory-new-p2");
        Assert.Contains(result.Variants, v => v.Slug == "karambit-doppler-factory-new-ruby");
    }

    [Fact]
    public async Task FetchAsync_DopplerItem_PhaseVariantMetadataContainsPhase()
    {
        var fetcher = BuildFetcherWithJson(DopplerItemJson());

        var result = await fetcher.FetchAsync();

        var phase2 = result.Variants.Single(v => v.Slug == "karambit-doppler-factory-new-p2");
        Assert.Equal("Phase 2", phase2.Metadata["phase"]);
    }

    [Fact]
    public async Task FetchAsync_DopplerItem_BaseVariantHasNoPhaseInMetadata()
    {
        var fetcher = BuildFetcherWithJson(DopplerItemJson());

        var result = await fetcher.FetchAsync();

        var baseVariant = result.Variants.Single(v => v.Slug == "karambit-doppler-factory-new");
        Assert.False(baseVariant.Metadata.ContainsKey("phase"));
    }

    [Fact]
    public async Task FetchAsync_DopplerItem_AllVariantsReferenceCorrectItemId()
    {
        var fetcher = BuildFetcherWithJson(DopplerItemJson());

        var result = await fetcher.FetchAsync();

        var itemId = result.Items[0].Id;
        Assert.All(result.Variants, v => Assert.Equal(itemId, v.ItemId));
    }

    #endregion

    #region Prices

    [Fact]
    public async Task FetchAsync_ValidItem_ProducesPriceRowsForEachPresentMarketplace()
    {
        // JSON has buff (ask+bid) and steam (ask only) → 3 price rows
        var fetcher = BuildFetcherWithJson(SingleItemJson());

        var result = await fetcher.FetchAsync();

        Assert.Equal(3, result.Prices.Count);
    }

    [Fact]
    public async Task FetchAsync_ValidItem_BuffAskPriceCorrect()
    {
        var fetcher = BuildFetcherWithJson(SingleItemJson(buffAsk: 34.30m));

        var result = await fetcher.FetchAsync();

        var price = result.Prices.Single(p =>
            p.Source == Sources.CS2ShBuff && p.PriceType == PriceTypes.LowestListing);
        Assert.Equal(34.30m, price.Price);
    }

    [Fact]
    public async Task FetchAsync_ValidItem_BuffBidPriceCorrect()
    {
        var fetcher = BuildFetcherWithJson(SingleItemJson(buffBid: 32.86m));

        var result = await fetcher.FetchAsync();

        var price = result.Prices.Single(p =>
            p.Source == Sources.CS2ShBuff && p.PriceType == PriceTypes.BuyOrder);
        Assert.Equal(32.86m, price.Price);
    }

    [Fact]
    public async Task FetchAsync_ValidItem_BuffAskVolumeAttachedToCorrectPrice()
    {
        var fetcher = BuildFetcherWithJson(SingleItemJson(buffAskVolume: 10444));

        var result = await fetcher.FetchAsync();

        var price = result.Prices.Single(p =>
            p.Source == Sources.CS2ShBuff && p.PriceType == PriceTypes.LowestListing);
        Assert.Equal(10444, price.Volume);
    }

    [Fact]
    public async Task FetchAsync_NullBid_BuyOrderRowOmitted()
    {
        var fetcher = BuildFetcherWithJson(NullBidJson());

        var result = await fetcher.FetchAsync();

        Assert.DoesNotContain(result.Prices, p =>
            p.Source == Sources.CS2ShBuff && p.PriceType == PriceTypes.BuyOrder);
    }

    [Fact]
    public async Task FetchAsync_AskOnlySource_OnlyLowestListingProduced()
    {
        // csfloat has no bid field
        var fetcher = BuildFetcherWithJson(NullBidJson());

        var result = await fetcher.FetchAsync();

        var csFloatPrices = result.Prices.Where(p => p.Source == Sources.CS2ShCsFloat).ToList();
        Assert.Single(csFloatPrices);
        Assert.Equal(PriceTypes.LowestListing, csFloatPrices[0].PriceType);
    }

    [Fact]
    public async Task FetchAsync_MissingMarketplace_NoPricesForThatSource()
    {
        // JSON only has buff and steam — no youpin, csfloat, skinport, c5game
        var fetcher = BuildFetcherWithJson(SingleItemJson());

        var result = await fetcher.FetchAsync();

        Assert.DoesNotContain(result.Prices, p => p.Source == Sources.CS2ShYoupin);
        Assert.DoesNotContain(result.Prices, p => p.Source == Sources.CS2ShSkinport);
        Assert.DoesNotContain(result.Prices, p => p.Source == Sources.CS2ShC5Game);
    }

    [Fact]
    public async Task FetchAsync_ValidItem_AllPricesCurrencyUsd()
    {
        var fetcher = BuildFetcherWithJson(SingleItemJson());

        var result = await fetcher.FetchAsync();

        Assert.All(result.Prices, p => Assert.Equal("USD", p.Currency));
    }

    #endregion

    #region Metadata

    [Fact]
    public async Task FetchAsync_ValidItem_VariantMetadataContainsWearAndFlags()
    {
        var fetcher = BuildFetcherWithJson(SingleItemJson());

        var result = await fetcher.FetchAsync();
        var variant = result.Variants[0];

        Assert.Equal("Field-Tested", variant.Metadata["wear"]);
        Assert.Equal(false, variant.Metadata["stattrak"]);
        Assert.Equal(false, variant.Metadata["souvenir"]);
    }

    [Fact]
    public async Task FetchAsync_ValidItem_ItemIsMarketableAndTradeable()
    {
        var fetcher = BuildFetcherWithJson(SingleItemJson());

        var result = await fetcher.FetchAsync();

        Assert.True(result.Items[0].IsMarketable);
        Assert.True(result.Items[0].IsTradeable);
    }

    #endregion

    #region File-Based

    [Fact(Skip = "Requires downloaded cs2.sh response file. See GitHub issue.")]
    public async Task FetchAsync_RealResponseFile_ParsesWithoutErrors()
    {
        var json = await File.ReadAllTextAsync("TestData/CS2Sh/prices_response.json");
        var fetcher = BuildFetcherWithJson(json);

        var result = await fetcher.FetchAsync();

        Assert.True(result.IsSuccess);
        Assert.NotEmpty(result.Items);
        Assert.NotEmpty(result.Variants);
        Assert.NotEmpty(result.Prices);
    }

    #endregion
}