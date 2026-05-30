using System.IO.Compression;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Skindexer.Contracts.Constants;
using Skindexer.Contracts.Models;
using Skindexer.Fetchers.Games.CS2.Fetchers.Skinport;
using Skindexer.Fetchers.Games.CS2.SlugHelpers;
using Skindexer.Fetchers.Interfaces;

namespace Skindexer.Fetchers.Games.CS2.Fetchers.SkinportFetcher;

/// <summary>
/// Fetches live CS2 skin prices from Skinport's public bulk endpoint.
/// </summary>
public sealed class CS2SkinportFetcher : IScheduledFetcher
{
    public static readonly FetcherDescriptor Descriptor = new()
    {
        FetcherId = "cs2-skinport",
        Register = (services, _) =>
        {
            services.AddHttpClient<CS2SkinportFetcher>();
            services.AddSingleton<IGameFetcher, CS2SkinportFetcher>();
        }
    };

    private const string GameId = GameIds.CounterStrike;
    private const string BaseUrl = "https://api.skinport.com";
    private const int AppId = 730;
    private const string Currency = "USD";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly HttpClient _http;
    private readonly ILogger<CS2SkinportFetcher> _logger;

    public CS2SkinportFetcher(
        IHttpClientFactory httpClientFactory,
        ILogger<CS2SkinportFetcher> logger)
    {
        _http = httpClientFactory.CreateClient(nameof(CS2SkinportFetcher));
        _logger = logger;
    }

    public string FetcherId => Descriptor.FetcherId;
    public string DisplayName => "CS2 Skinport Price Fetcher";
    public string DefaultCronExpression => "0 2 * * *"; // 2:00 AM daily
    public bool IsAuthoritativeItemSource => false;

    // --- Core Execution ---

    public async Task<FetchResult> FetchAsync(CancellationToken cancellationToken = default)
    {
        var warnings = new List<string>();

        List<SkinportItemDto>? rawItems;
        try
        {
            rawItems = await FetchRawAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{Fetcher}] HTTP fetch failed", FetcherId);
            return FetchResult.Failure(GameId, Sources.Skinport, ex.Message);
        }

        if (rawItems is null || rawItems.Count == 0)
        {
            const string msg = "Response was empty or null";
            _logger.LogWarning("[{Fetcher}] {Message}", FetcherId, msg);
            return FetchResult.Failure(GameId, Sources.Skinport, msg);
        }

        _logger.LogInformation(
            "[{Fetcher}] Received {Count} items from Skinport API",
            FetcherId, rawItems.Count);

        var prices = new List<SkinPrice>();
        int skippedNoWear = 0;
        int skipped = 0;
        var recordedAt = DateTime.UtcNow;

        foreach (var item in rawItems)
        {
            if (!CS2MarketHashNameParser.TryParse(
                    item.MarketHashName,
                    out var weapon,
                    out var skinName,
                    out var wear,
                    out var isStatTrak,
                    out var isSouvenir))
            {
                skippedNoWear++;
                continue;
            }

            var slug = CS2SlugBuilder.BuildVariantSlug(weapon, skinName, wear, isStatTrak, isSouvenir);
            var currency = item.Currency ?? Currency;

            prices.AddRange(ExpandPrices(slug, currency, item, recordedAt));
        }

        if (skipped > 0)
            warnings.Add($"{skipped} items skipped due to unresolvable market_hash_name");

        _logger.LogInformation(
            "[{Fetcher}] Mapped {PriceCount} price records. " +
            "Skipped {SkippedNoWear} non-wear items (agents/keys/etc), {Skipped} unresolvable.",
            FetcherId, prices.Count, skippedNoWear, skipped);

        return warnings.Count > 0
            ? FetchResult.Partial(GameId, Sources.Skinport, [], [], prices, warnings)
            : FetchResult.Success(GameId, Sources.Skinport, [], [], prices);
    }

    // --- Compressed Network Streams ---

    private async Task<List<SkinportItemDto>?> FetchRawAsync(CancellationToken ct)
    {
        var url = $"{BaseUrl}/v1/items?app_id={AppId}&currency={Currency}";

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.TryAddWithoutValidation("Accept-Encoding", "br");

        using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();

        await using var compressed = await response.Content.ReadAsStreamAsync(ct);
        await using var decompressed = new BrotliStream(compressed, CompressionMode.Decompress);

        return await JsonSerializer.DeserializeAsync<List<SkinportItemDto>>(decompressed, JsonOptions, ct);
    }

    private static IEnumerable<SkinPrice> ExpandPrices(
        string slug,
        string currency,
        SkinportItemDto item,
        DateTime recordedAt)
    {
        if (item.MinPrice is { } lowest) yield return Make(slug, currency, PriceTypes.LowestListing, lowest, item.Quantity, recordedAt);
        if (item.SuggestedPrice is { } suggested) yield return Make(slug, currency, PriceTypes.SuggestedAvg, suggested, null, recordedAt);
        if (item.MeanPrice is { } mean) yield return Make(slug, currency, PriceTypes.Avg7d, mean, null, recordedAt);
        if (item.MedianPrice is { } median) yield return Make(slug, currency, PriceTypes.Median7d, median, null, recordedAt);
    }

    private static SkinPrice Make(
        string slug, 
        string currency, 
        string priceType,
        decimal price, 
        int? volume, 
        DateTime recordedAt) => new()
    {
        VariantId = Guid.Empty,
        GameId = GameId,
        Slug = slug,
        Source = Sources.Skinport,
        PriceType = priceType,
        Price = price,
        Currency = currency,
        Volume = volume,
        RecordedAt = recordedAt,
    };
}