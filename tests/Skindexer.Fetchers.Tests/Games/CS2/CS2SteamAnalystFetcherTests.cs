using System.Net;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Skindexer.Contracts.Constants;
using Skindexer.Fetchers.Games.CS2.Fetchers.SteamAnalyst;

namespace Skindexer.Fetchers.Tests.Games.CS2;

public class CS2SteamAnalystFetcherTests
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

    private static CS2SteamAnalystFetcher CreateFetcher(string responseJson, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        var handler    = new FakeHttpMessageHandler(responseJson, statusCode);
        var httpClient = new HttpClient(handler);

        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient(Arg.Any<string>()).Returns(httpClient);

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["SteamAnalyst:ApiKey"] = "test-api-key",
            })
            .Build();

        return new CS2SteamAnalystFetcher(factory, configuration, NullLogger<CS2SteamAnalystFetcher>.Instance);
    }

    private static CS2SteamAnalystFetcher CreateFetcherFromFile()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "TestData", "SteamAnalyst", "prices_response.json");
        var json = File.ReadAllText(path);
        return CreateFetcher(json);
    }

    /// <summary>
    /// Two regular weapon skin variants and one agent (no wear — silently skipped).
    /// </summary>
    private static string MinimalValidResponse() => """
        [
          {
            "market_name": "AK-47 | Redline (Field-Tested)",
            "avg_price_7_days": "10.50",
            "avg_price_7_days_raw": 10.50,
            "avg_price_30_days": "11.20",
            "avg_price_30_days_raw": 11.20,
            "sold_last_24h": "15",
            "sold_last_7d": "90",
            "avg_daily_volume": "13",
            "rarity": "Classified Rifle",
            "ongoing_price_manipulation": "0"
          },
          {
            "market_name": "StatTrak\u2122 AWP | Asiimov (Field-Tested)",
            "avg_price_7_days": "85.00",
            "avg_price_7_days_raw": 85.00,
            "avg_price_30_days": "88.00",
            "avg_price_30_days_raw": 88.00,
            "sold_last_24h": "5",
            "sold_last_7d": "30",
            "avg_daily_volume": "4",
            "rarity": "Covert Sniper Rifle",
            "ongoing_price_manipulation": "0"
          },
          {
            "market_name": "Sir Bloody Miami Darryl | The Professionals",
            "avg_price_7_days": "5.00",
            "avg_price_7_days_raw": 5.00,
            "rarity": "Superior Agent",
            "ongoing_price_manipulation": "0"
          }
        ]
        """;

    /// <summary>
    /// Rare item (knife) with suggested amounts instead of avg prices.
    /// </summary>
    private static string ResponseWithRareItem() => """
        [
          {
            "market_name": "★ Karambit | Fade (Factory New)",
            "suggested_amount_avg": "583.75",
            "suggested_amount_avg_raw": 583.75,
            "suggested_amount_min": "567.50",
            "suggested_amount_min_raw": 567.50,
            "suggested_amount_max": "600.00",
            "suggested_amount_max_raw": 600.00,
            "sold_last_24h": "1",
            "sold_last_7d": "8",
            "rarity": "\u2605 Covert Knife",
            "ongoing_price_manipulation": "0"
          }
        ]
        """;

    /// <summary>
    /// Item flagged for price manipulation — avg fields absent, safe_price present.
    /// </summary>
    private static string ResponseWithManipulatedItem() => """
        [
          {
            "market_name": "XM1014 | Jungle (Well-Worn)",
            "safe_price": "0.58",
            "safe_price_raw": 0.58,
            "sold_last_24h": 3,
            "sold_last_7d": 56,
            "rarity": "Consumer Grade Shotgun",
            "ongoing_price_manipulation": "1"
          }
        ]
        """;

    /// <summary>
    /// Manipulated item with no safe_price — should produce zero price rows.
    /// </summary>
    private static string ResponseWithManipulatedItemNoSafePrice() => """
        [
          {
            "market_name": "AWP | Redline (Field-Tested)",
            "sold_last_7d": 10,
            "rarity": "Classified Sniper Rifle",
            "ongoing_price_manipulation": "1"
          }
        ]
        """;

    /// <summary>
    /// Souvenir skin to verify souvenir slug generation.
    /// </summary>
    private static string ResponseWithSouvenirSkin() => """
        [
          {
            "market_name": "Souvenir AK-47 | Redline (Factory New)",
            "avg_price_7_days_raw": 120.00,
            "avg_price_30_days_raw": 125.00,
            "sold_last_7d": 2,
            "ongoing_price_manipulation": "0"
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
        Assert.Equal("cs2-steamanalyst", fetcher.FetcherId);
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
            new CS2SteamAnalystFetcher(factory, configuration, NullLogger<CS2SteamAnalystFetcher>.Instance));
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

        Assert.Equal(Sources.SteamAnalyst, result.Source);
    }

    [Fact]
    public async Task FetchAsync_ValidResponse_ItemsAndVariantsAreEmpty()
    {
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
    public async Task FetchAsync_ValidResponse_AllPricesHaveCorrectSource()
    {
        var fetcher = CreateFetcher(MinimalValidResponse());

        var result = await fetcher.FetchAsync();

        Assert.All(result.Prices, p => Assert.Equal(Sources.SteamAnalyst, p.Source));
    }

    [Fact]
    public async Task FetchAsync_ValidResponse_AllPricesHavePositivePrice()
    {
        var fetcher = CreateFetcher(MinimalValidResponse());

        var result = await fetcher.FetchAsync();

        Assert.All(result.Prices, p => Assert.True(p.Price > 0));
    }

    [Fact]
    public async Task FetchAsync_ValidResponse_AllPricesHaveNonEmptySlug()
    {
        var fetcher = CreateFetcher(MinimalValidResponse());

        var result = await fetcher.FetchAsync();

        Assert.All(result.Prices, p => Assert.False(string.IsNullOrWhiteSpace(p.Slug)));
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
        var expectedSlug = "awp-asiimov-stattrak-field-tested";

        var result = await fetcher.FetchAsync();

        Assert.Contains(result.Prices, p => p.Slug == expectedSlug);
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

    #region Price Expansion — Regular Items

    [Fact]
    public async Task FetchAsync_RegularItem_ProducesAvg7dRow()
    {
        var fetcher = CreateFetcher(MinimalValidResponse());

        var result = await fetcher.FetchAsync();

        var akPrices = result.Prices.Where(p => p.Slug == "ak-47-redline-field-tested").ToList();

        Assert.Contains(akPrices, p => p.PriceType == PriceTypes.Avg7d);
    }

    [Fact]
    public async Task FetchAsync_RegularItem_ProducesAvg30dRow()
    {
        var fetcher = CreateFetcher(MinimalValidResponse());

        var result = await fetcher.FetchAsync();

        var akPrices = result.Prices.Where(p => p.Slug == "ak-47-redline-field-tested").ToList();

        Assert.Contains(akPrices, p => p.PriceType == PriceTypes.Avg30d);
    }

    [Fact]
    public async Task FetchAsync_RegularItem_Avg7dRowHasVolume()
    {
        var fetcher = CreateFetcher(MinimalValidResponse());

        var result = await fetcher.FetchAsync();

        var avg7dRow = result.Prices
            .Single(p => p.Slug == "ak-47-redline-field-tested" && p.PriceType == PriceTypes.Avg7d);

        Assert.Equal(90, avg7dRow.Volume);
    }

    [Fact]
    public async Task FetchAsync_RegularItem_Avg30dRowHasNullVolume()
    {
        var fetcher = CreateFetcher(MinimalValidResponse());

        var result = await fetcher.FetchAsync();

        var avg30dRow = result.Prices
            .Single(p => p.Slug == "ak-47-redline-field-tested" && p.PriceType == PriceTypes.Avg30d);

        Assert.Null(avg30dRow.Volume);
    }

    #endregion

    #region Price Expansion — Rare Items

    [Fact]
    public async Task FetchAsync_RareItem_ProducesSuggestedAvgRow()
    {
        var fetcher = CreateFetcher(ResponseWithRareItem());

        var result = await fetcher.FetchAsync();

        Assert.Contains(result.Prices, p => p.PriceType == PriceTypes.SuggestedAvg);
    }

    [Fact]
    public async Task FetchAsync_RareItem_ProducesSuggestedMinRow()
    {
        var fetcher = CreateFetcher(ResponseWithRareItem());

        var result = await fetcher.FetchAsync();

        Assert.Contains(result.Prices, p => p.PriceType == PriceTypes.SuggestedMin);
    }

    [Fact]
    public async Task FetchAsync_RareItem_ProducesSuggestedMaxRow()
    {
        var fetcher = CreateFetcher(ResponseWithRareItem());

        var result = await fetcher.FetchAsync();

        Assert.Contains(result.Prices, p => p.PriceType == PriceTypes.SuggestedMax);
    }

    [Fact]
    public async Task FetchAsync_RareItem_SuggestedAvgHasCorrectPrice()
    {
        var fetcher = CreateFetcher(ResponseWithRareItem());

        var result = await fetcher.FetchAsync();

        var suggestedAvgRow = result.Prices.Single(p => p.PriceType == PriceTypes.SuggestedAvg);

        Assert.Equal(583.75m, suggestedAvgRow.Price);
    }

    #endregion

    #region Price Manipulation

    [Fact]
    public async Task FetchAsync_ManipulatedItem_ProducesSafePriceRow()
    {
        var fetcher = CreateFetcher(ResponseWithManipulatedItem());

        var result = await fetcher.FetchAsync();

        Assert.Contains(result.Prices, p => p.PriceType == PriceTypes.SafePrice);
    }

    [Fact]
    public async Task FetchAsync_ManipulatedItem_DoesNotProduceAvgRows()
    {
        var fetcher = CreateFetcher(ResponseWithManipulatedItem());

        var result = await fetcher.FetchAsync();

        Assert.DoesNotContain(result.Prices, p => p.PriceType == PriceTypes.Avg7d);
        Assert.DoesNotContain(result.Prices, p => p.PriceType == PriceTypes.Avg30d);
    }

    [Fact]
    public async Task FetchAsync_ManipulatedItem_SafePriceHasCorrectValue()
    {
        var fetcher = CreateFetcher(ResponseWithManipulatedItem());

        var result = await fetcher.FetchAsync();

        var safePriceRow = result.Prices.Single(p => p.PriceType == PriceTypes.SafePrice);

        Assert.Equal(0.58m, safePriceRow.Price);
    }

    [Fact]
    public async Task FetchAsync_ManipulatedItemWithNoSafePrice_ProducesNoPriceRows()
    {
        var fetcher = CreateFetcher(ResponseWithManipulatedItemNoSafePrice());

        var result = await fetcher.FetchAsync();

        Assert.Empty(result.Prices);
    }

    [Fact]
    public async Task FetchAsync_ManipulatedItemWithNoSafePrice_StillReturnsSuccess()
    {
        var fetcher = CreateFetcher(ResponseWithManipulatedItemNoSafePrice());

        var result = await fetcher.FetchAsync();

        // No prices is valid — it just means we had nothing safe to store
        Assert.True(result.IsSuccess);
    }

    #endregion

    #region Non-Parseable Items

    [Fact]
    public async Task FetchAsync_AgentEntry_ProducesNoPrices()
    {
        var fetcher = CreateFetcher(MinimalValidResponse());

        var result = await fetcher.FetchAsync();

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

    [Fact(Skip = "Requires downloaded SteamAnalyst response file. See GitHub issue #6.")]
    public async Task FetchAsync_RealResponseFile_ReturnsSuccess()
    {
        var fetcher = CreateFetcherFromFile();

        var result = await fetcher.FetchAsync();

        Assert.True(result.IsSuccess);
    }

    [Fact(Skip = "Requires downloaded SteamAnalyst response file. See GitHub issue #6.")]
    public async Task FetchAsync_RealResponseFile_ReturnsPrices()
    {
        var fetcher = CreateFetcherFromFile();

        var result = await fetcher.FetchAsync();

        Assert.NotEmpty(result.Prices);
    }

    [Fact(Skip = "Requires downloaded SteamAnalyst response file. See GitHub issue #6.")]
    public async Task FetchAsync_RealResponseFile_AllPricesHaveNonEmptySlug()
    {
        var fetcher = CreateFetcherFromFile();

        var result = await fetcher.FetchAsync();

        Assert.All(result.Prices, p => Assert.False(string.IsNullOrWhiteSpace(p.Slug)));
    }

    [Fact(Skip = "Requires downloaded SteamAnalyst response file. See GitHub issue #6.")]
    public async Task FetchAsync_RealResponseFile_AllPricesHavePositivePrice()
    {
        var fetcher = CreateFetcherFromFile();

        var result = await fetcher.FetchAsync();

        Assert.All(result.Prices, p => Assert.True(p.Price > 0));
    }

    [Fact(Skip = "Requires downloaded SteamAnalyst response file. See GitHub issue #6.")]
    public async Task FetchAsync_RealResponseFile_AllPricesHaveGuidEmptyVariantId()
    {
        var fetcher = CreateFetcherFromFile();

        var result = await fetcher.FetchAsync();

        Assert.All(result.Prices, p => Assert.Equal(Guid.Empty, p.VariantId));
    }

    [Fact(Skip = "Requires downloaded SteamAnalyst response file. See GitHub issue #6.")]
    public async Task FetchAsync_RealResponseFile_AllPricesHaveCorrectSource()
    {
        var fetcher = CreateFetcherFromFile();

        var result = await fetcher.FetchAsync();

        Assert.All(result.Prices, p => Assert.Equal(Sources.SteamAnalyst, p.Source));
    }

    #endregion
}