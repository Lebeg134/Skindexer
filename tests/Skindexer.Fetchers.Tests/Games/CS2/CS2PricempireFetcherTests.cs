using System.Net;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Skindexer.Contracts.Constants;
using Skindexer.Fetchers.Games.CS2.Fetchers;

namespace Skindexer.Fetchers.Tests.Games.CS2;

public class CS2PricempireFetcherTests
{
    #region Test Data Builders

    private sealed class FakeHttpMessageHandler(string responseJson, HttpStatusCode statusCode = HttpStatusCode.OK)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(responseJson, Encoding.UTF8, "application/json"),
            };
            return Task.FromResult(response);
        }
    }

    private static CS2PricempireFetcher CreateFetcher(string responseJson, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        var handler    = new FakeHttpMessageHandler(responseJson, statusCode);
        var httpClient = new HttpClient(handler);

        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient(Arg.Any<string>()).Returns(httpClient);

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Pricempire:ApiKey"] = "test-api-key",
            })
            .Build();

        return new CS2PricempireFetcher(factory, configuration, NullLogger<CS2PricempireFetcher>.Instance);
    }

    private static CS2PricempireFetcher CreateFetcherFromFile()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "TestData", "Pricempire", "prices_response.json");
        var json = File.ReadAllText(path);
        return CreateFetcher(json);
    }

    /// <summary>
    /// Minimal valid response with two weapon skin variants (one plain, one StatTrak)
    /// and one agent entry (no wear — should be skipped silently).
    /// </summary>
    private static string MinimalValidResponse() => """
        [
          {
            "market_hash_name": "AK-47 | Redline (Field-Tested)",
            "liquidity": 85,
            "count": 5000,
            "prices": [
              {
                "provider_key": "buff163",
                "price": 10.50,
                "count": 120,
                "updated_at": "2025-01-15T12:00:00.000Z",
                "avg_7": 10.80,
                "avg_30": 11.20,
                "median_7": 10.70,
                "median_30": 11.00
              },
              {
                "provider_key": "steam",
                "price": 12.00,
                "count": 800,
                "updated_at": "2025-01-15T12:00:00.000Z",
                "avg_7": null,
                "avg_30": null,
                "median_7": null,
                "median_30": null
              }
            ]
          },
          {
            "market_hash_name": "StatTrak\u2122 AK-47 | Redline (Field-Tested)",
            "liquidity": 60,
            "count": 800,
            "prices": [
              {
                "provider_key": "buff163",
                "price": 25.00,
                "count": 40,
                "updated_at": "2025-01-15T12:00:00.000Z",
                "avg_7": 25.50,
                "avg_30": null,
                "median_7": null,
                "median_30": null
              }
            ]
          },
          {
            "market_hash_name": "Sir Bloody Miami Darryl | The Professionals",
            "liquidity": 10,
            "count": 50,
            "prices": [
              {
                "provider_key": "steam",
                "price": 5.00,
                "count": 10,
                "updated_at": "2025-01-15T12:00:00.000Z"
              }
            ]
          }
        ]
        """;

    /// <summary>
    /// Response where a provider entry has a null price field — should produce
    /// no LowestListing row but still produce rows for non-null avg/median fields.
    /// </summary>
    private static string ResponseWithNullPriceField() => """
        [
          {
            "market_hash_name": "AWP | Asiimov (Field-Tested)",
            "liquidity": 90,
            "count": 3000,
            "prices": [
              {
                "provider_key": "dmarket",
                "price": null,
                "count": null,
                "updated_at": "2025-01-15T12:00:00.000Z",
                "avg_7": 55.00,
                "avg_30": 56.00,
                "median_7": null,
                "median_30": null
              }
            ]
          }
        ]
        """;

    /// <summary>
    /// Response with a Souvenir skin to verify souvenir slug generation.
    /// </summary>
    private static string ResponseWithSouvenirSkin() => """
        [
          {
            "market_hash_name": "Souvenir AK-47 | Redline (Factory New)",
            "liquidity": 20,
            "count": 15,
            "prices": [
              {
                "provider_key": "steam",
                "price": 350.00,
                "count": 3,
                "updated_at": "2025-01-15T12:00:00.000Z"
              }
            ]
          }
        ]
        """;

    #endregion

    #region Construction

    [Fact]
    public void Constructor_WithValidConfiguration_CreatesSuccessfully()
    {
        var fetcher = CreateFetcher(MinimalValidResponse());

        Assert.NotNull(fetcher);
        Assert.Equal("cs2-pricempire", fetcher.FetcherId);
    }

    [Fact]
    public void Constructor_MissingApiKey_ThrowsInvalidOperationException()
    {
        var handler    = new FakeHttpMessageHandler("[]");
        var httpClient = new HttpClient(handler);

        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient(Arg.Any<string>()).Returns(httpClient);

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        Assert.Throws<InvalidOperationException>(() =>
            new CS2PricempireFetcher(factory, configuration, NullLogger<CS2PricempireFetcher>.Instance));
    }

    #endregion

    #region Guard Clauses — HTTP Failures

    [Fact]
    public async Task FetchAsync_NonSuccessStatusCode_ReturnsFailure()
    {
        var fetcher = CreateFetcher("{}", HttpStatusCode.Unauthorized);

        var result = await fetcher.FetchAsync();

        Assert.False(result.IsSuccess);
        Assert.NotNull(result.ErrorMessage);
    }

    [Fact]
    public async Task FetchAsync_EmptyArray_ReturnsFailure()
    {
        var fetcher = CreateFetcher("[]");

        var result = await fetcher.FetchAsync();

        Assert.False(result.IsSuccess);
    }

    #endregion

    #region Result Shape

    [Fact]
    public async Task FetchAsync_ValidResponse_ReturnsSuccess()
    {
        var fetcher = CreateFetcher(MinimalValidResponse());

        var result = await fetcher.FetchAsync();

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task FetchAsync_ValidResponse_ReturnsCorrectGameId()
    {
        var fetcher = CreateFetcher(MinimalValidResponse());

        var result = await fetcher.FetchAsync();

        Assert.Equal("cs2", result.GameId);
    }

    [Fact]
    public async Task FetchAsync_ValidResponse_ReturnsCorrectSource()
    {
        var fetcher = CreateFetcher(MinimalValidResponse());

        var result = await fetcher.FetchAsync();

        Assert.Equal(Sources.Pricempire, result.Source);
    }

    [Fact]
    public async Task FetchAsync_ValidResponse_ItemsAndVariantsAreEmpty()
    {
        // Pricempire is a prices-only fetcher
        var fetcher = CreateFetcher(MinimalValidResponse());

        var result = await fetcher.FetchAsync();

        Assert.Empty(result.Items);
        Assert.Empty(result.Variants);
    }

    [Fact]
    public async Task FetchAsync_ValidResponse_ReturnsPrices()
    {
        var fetcher = CreateFetcher(MinimalValidResponse());

        var result = await fetcher.FetchAsync();

        Assert.NotEmpty(result.Prices);
    }

    [Fact]
    public async Task FetchAsync_ValidResponse_AllPricesHaveGuidEmptyVariantId()
    {
        var fetcher = CreateFetcher(MinimalValidResponse());

        var result = await fetcher.FetchAsync();

        Assert.All(result.Prices, p => Assert.Equal(Guid.Empty, p.VariantId));
    }

    [Fact]
    public async Task FetchAsync_ValidResponse_AllPricesHaveUsdCurrency()
    {
        var fetcher = CreateFetcher(MinimalValidResponse());

        var result = await fetcher.FetchAsync();

        Assert.All(result.Prices, p => Assert.Equal("USD", p.Currency));
    }

    [Fact]
    public async Task FetchAsync_ValidResponse_AllPricesHaveCorrectGameId()
    {
        var fetcher = CreateFetcher(MinimalValidResponse());

        var result = await fetcher.FetchAsync();

        Assert.All(result.Prices, p => Assert.Equal("cs2", p.GameId));
    }

    [Fact]
    public async Task FetchAsync_ValidResponse_AllPricesHaveNonEmptySlug()
    {
        var fetcher = CreateFetcher(MinimalValidResponse());

        var result = await fetcher.FetchAsync();

        Assert.All(result.Prices, p => Assert.False(string.IsNullOrWhiteSpace(p.Slug)));
    }

    [Fact]
    public async Task FetchAsync_ValidResponse_AllPricesHavePositivePrice()
    {
        var fetcher = CreateFetcher(MinimalValidResponse());

        var result = await fetcher.FetchAsync();

        Assert.All(result.Prices, p => Assert.True(p.Price > 0));
    }

    #endregion

    #region Slug Generation

    [Fact]
    public async Task FetchAsync_PlainSkin_ProducesCorrectSlug()
    {
        var fetcher      = CreateFetcher(MinimalValidResponse());
        var expectedSlug = "ak-47-redline-field-tested";

        var result = await fetcher.FetchAsync();

        Assert.Contains(result.Prices, p => p.Slug == expectedSlug);
    }

    [Fact]
    public async Task FetchAsync_StatTrakSkin_ProducesCorrectSlug()
    {
        var fetcher      = CreateFetcher(MinimalValidResponse());
        var expectedSlug = "ak-47-redline-stattrak-field-tested";

        var result = await fetcher.FetchAsync();

        Assert.Contains(result.Prices, p => p.Slug == expectedSlug);
    }

    [Fact]
    public async Task FetchAsync_StatTrakSkin_DoesNotProducePlainSlug()
    {
        var fetcher         = CreateFetcher(MinimalValidResponse());
        var nonStatTrakSlug = "ak-47-redline-field-tested";
        var statTrakSlug    = "ak-47-redline-stattrak-field-tested";

        var result = await fetcher.FetchAsync();

        var plainPrices   = result.Prices.Where(p => p.Slug == nonStatTrakSlug).ToList();
        var statTrakPrices = result.Prices.Where(p => p.Slug == statTrakSlug).ToList();

        // Plain slug prices exist (from the non-StatTrak item), but none of them
        // should originate from the StatTrak market_hash_name entry
        Assert.NotEmpty(statTrakPrices);
        Assert.DoesNotContain(statTrakPrices, p => p.Slug == nonStatTrakSlug);
    }

    [Fact]
    public async Task FetchAsync_SouvenirSkin_ProducesCorrectSlug()
    {
        var fetcher      = CreateFetcher(ResponseWithSouvenirSkin());
        var expectedSlug = "ak-47-redline-souvenir-factory-new";

        var result = await fetcher.FetchAsync();

        Assert.Contains(result.Prices, p => p.Slug == expectedSlug);
    }

    #endregion

    #region Price Expansion

    [Fact]
    public async Task FetchAsync_ProviderWithAllPriceFields_ProducesMultiplePriceRows()
    {
        var fetcher = CreateFetcher(MinimalValidResponse());

        var result = await fetcher.FetchAsync();

        // buff163 entry for AK-47 | Redline has price + avg_7 + avg_30 + median_7 + median_30 = 5 rows
        var buff163Prices = result.Prices
            .Where(p => p.Slug == "ak-47-redline-field-tested" && p.Source == Sources.PricempireBuff163)
            .ToList();

        Assert.Equal(5, buff163Prices.Count);
    }

    [Fact]
    public async Task FetchAsync_ProviderWithAllNullAvgFields_ProducesOnlyLowestListingRow()
    {
        var fetcher = CreateFetcher(MinimalValidResponse());

        var result = await fetcher.FetchAsync();

        // steam entry for AK-47 | Redline has only price; all avg/median fields are null
        var steamPrices = result.Prices
            .Where(p => p.Slug == "ak-47-redline-field-tested" && p.Source == Sources.PricempireSteam)
            .ToList();

        Assert.Single(steamPrices);
        Assert.Equal(PriceTypes.LowestListing, steamPrices[0].PriceType);
    }

    [Fact]
    public async Task FetchAsync_NullPriceField_DoesNotProduceLowestListingRow()
    {
        var fetcher = CreateFetcher(ResponseWithNullPriceField());

        var result = await fetcher.FetchAsync();

        var lowestListingRows = result.Prices
            .Where(p => p.PriceType == PriceTypes.LowestListing)
            .ToList();

        Assert.Empty(lowestListingRows);
    }

    [Fact]
    public async Task FetchAsync_NullPriceButNonNullAvgFields_ProducesAvgRows()
    {
        var fetcher = CreateFetcher(ResponseWithNullPriceField());

        var result = await fetcher.FetchAsync();

        // dmarket entry has null price but avg_7: 55.00 and avg_30: 56.00
        var avgPrices = result.Prices
            .Where(p => p.PriceType == PriceTypes.Avg7d || p.PriceType == PriceTypes.Avg30d)
            .ToList();

        Assert.Equal(2, avgPrices.Count);
    }

    [Fact]
    public async Task FetchAsync_MultipleProviders_EachGetsItsOwnSourceLabel()
    {
        var fetcher = CreateFetcher(MinimalValidResponse());

        var result = await fetcher.FetchAsync();

        var sources = result.Prices
            .Select(p => p.Source)
            .Distinct()
            .ToList();

        Assert.Contains(Sources.PricempireBuff163, sources);
        Assert.Contains(Sources.PricempireSteam, sources);
    }

    #endregion

    #region Non-Parseable Items

    [Fact]
    public async Task FetchAsync_AgentEntry_ProducesNoPrices()
    {
        var fetcher = CreateFetcher(MinimalValidResponse());

        var result = await fetcher.FetchAsync();

        // "Sir Bloody Miami Darryl | The Professionals" has no wear — should be silently skipped
        Assert.DoesNotContain(result.Prices, p => p.Slug.Contains("darryl"));
    }

    [Fact]
    public async Task FetchAsync_AgentEntry_DoesNotCauseFailure()
    {
        var fetcher = CreateFetcher(MinimalValidResponse());

        var result = await fetcher.FetchAsync();

        Assert.True(result.IsSuccess);
    }

    #endregion

    #region File-Based — Real Response Shape

    [Fact(Skip = "Requires downloaded Pricempire response file. See GitHub issue #5.")]
    public async Task FetchAsync_RealResponseFile_ReturnsSuccess()
    {
        var fetcher = CreateFetcherFromFile();

        var result = await fetcher.FetchAsync();

        Assert.True(result.IsSuccess);
    }

    [Fact(Skip = "Requires downloaded Pricempire response file. See GitHub issue #5.")]
    public async Task FetchAsync_RealResponseFile_ReturnsPrices()
    {
        var fetcher = CreateFetcherFromFile();

        var result = await fetcher.FetchAsync();

        Assert.NotEmpty(result.Prices);
    }

    [Fact(Skip = "Requires downloaded Pricempire response file. See GitHub issue #5.")]
    public async Task FetchAsync_RealResponseFile_AllPricesHaveNonEmptySlug()
    {
        var fetcher = CreateFetcherFromFile();

        var result = await fetcher.FetchAsync();

        Assert.All(result.Prices, p => Assert.False(string.IsNullOrWhiteSpace(p.Slug)));
    }

    [Fact(Skip = "Requires downloaded Pricempire response file. See GitHub issue #5.")]
    public async Task FetchAsync_RealResponseFile_AllPricesHaveKnownSource()
    {
        var fetcher = CreateFetcherFromFile();

        var knownSources = new HashSet<string>
        {
            Sources.PricempireBuff163,
            Sources.PricempireSteam,
            Sources.PricempireDMarket,
            Sources.PricempireSkinport,
            Sources.PricempireSkinbaron,
            Sources.PricempireCSFloat,
            Sources.PricempireWaxpeer,
        };

        var result = await fetcher.FetchAsync();

        var unknownSources = result.Prices
            .Select(p => p.Source)
            .Distinct()
            .Where(s => !knownSources.Contains(s))
            .ToList();

        Assert.Empty(unknownSources);
    }

    [Fact(Skip = "Requires downloaded Pricempire response file. See GitHub issue #5.")]
    public async Task FetchAsync_RealResponseFile_AllPricesHavePositivePrice()
    {
        var fetcher = CreateFetcherFromFile();

        var result = await fetcher.FetchAsync();

        Assert.All(result.Prices, p => Assert.True(p.Price > 0));
    }

    [Fact(Skip = "Requires downloaded Pricempire response file. See GitHub issue #5.")]
    public async Task FetchAsync_RealResponseFile_AllPricesHaveGuidEmptyVariantId()
    {
        var fetcher = CreateFetcherFromFile();

        var result = await fetcher.FetchAsync();

        Assert.All(result.Prices, p => Assert.Equal(Guid.Empty, p.VariantId));
    }

    #endregion
}
