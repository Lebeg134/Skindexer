using System.IO.Compression;
using System.Net.Http.Headers;
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
/// Implements IScheduledFetcher — runs daily on the FetchScheduler cycle.
///
/// Endpoint: GET https://api.skinport.com/v1/items?app_id=730&amp;currency=USD
///
/// Auth: None required.
/// Compression: Brotli (Accept-Encoding: br) — required; API returns 406 without it.
///
/// Prices-only fetcher: Items and Variants are always empty.
/// Item catalog comes from CS2ByMykelItemFetcher.
///
/// Slug resolution: market_hash_name is parsed by CS2MarketHashNameParser
/// then built into a canonical slug via CS2SlugBuilder.BuildVariantSlug.
/// VariantId is left as Guid.Empty — FetchResultPersister resolves it via slug map.
///
/// Non-wear items (agents, keys, music kits, etc.) are skipped silently;
/// the parser returns false for those and they are counted separately.
/// </summary>
public sealed class CS2SkinportFetcher : IScheduledFetcher
{
    // -------------------------------------------------------------------------
    // IGameFetcher / IScheduledFetcher
    // -------------------------------------------------------------------------

    public static readonly FetcherDescriptor Descriptor = new()
    {
        FetcherId = "cs2-skinport",
        Register  = (services, _) =>
        {
            services.AddHttpClient<CS2SkinportFetcher>();
            services.AddSingleton<IGameFetcher, CS2SkinportFetcher>();
        }
    };

    public string FetcherId   => Descriptor.FetcherId;
    public string DisplayName => "CS2 Skinport Price Fetcher";

    public bool     IsAuthoritativeItemSource { get; } = false;
    public string DefaultCronExpression => "0 2 * * *"; // 2:00 AM daily

    // -------------------------------------------------------------------------
    // Constants
    // -------------------------------------------------------------------------

    private const string BaseUrl  = "https://api.skinport.com";
    private const int    AppId    = 730;
    private const string Currency = "USD";

    // -------------------------------------------------------------------------
    // Dependencies
    // -------------------------------------------------------------------------

    private readonly HttpClient                    _http;
    private readonly ILogger<CS2SkinportFetcher>   _logger;

    // No IConfiguration needed — endpoint is public, no API key required.
    public CS2SkinportFetcher(
        IHttpClientFactory httpClientFactory,
        ILogger<CS2SkinportFetcher> logger)
    {
        _http   = httpClientFactory.CreateClient(nameof(CS2SkinportFetcher));
        _logger = logger;
    }

    // -------------------------------------------------------------------------
    // IGameFetcher
    // -------------------------------------------------------------------------

    public async Task<FetchResult> FetchAsync(CancellationToken cancellationToken = default)
    {
        var warnings = new List<string>();

        // --- 1. HTTP fetch ---
        List<SkinportItemDto>? rawItems;
        try
        {
            rawItems = await FetchRawAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{Fetcher}] HTTP fetch failed", FetcherId);
            return FetchResult.Failure("cs2", Sources.Skinport, ex.Message);
        }

        if (rawItems is null || rawItems.Count == 0)
        {
            const string msg = "Response was empty or null";
            _logger.LogWarning("[{Fetcher}] {Message}", FetcherId, msg);
            return FetchResult.Failure("cs2", Sources.Skinport, msg);
        }

        _logger.LogInformation(
            "[{Fetcher}] Received {Count} items from Skinport API",
            FetcherId, rawItems.Count);

        // --- 2. Map to SkinPrice records ---
        var prices        = new List<SkinPrice>();
        int skippedNoWear = 0;
        int skipped       = 0;
        var recordedAt    = DateTime.UtcNow;

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

            var slug     = CS2SlugBuilder.BuildVariantSlug(weapon, skinName, wear, isStatTrak, isSouvenir);
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
            ? FetchResult.Partial("cs2", Sources.Skinport, [], [], prices, warnings)
            : FetchResult.Success("cs2", Sources.Skinport, [], [], prices);
    }

    // -------------------------------------------------------------------------
    // HTTP
    // -------------------------------------------------------------------------

    private async Task<List<SkinportItemDto>?> FetchRawAsync(CancellationToken ct)
    {
        var url = $"{BaseUrl}/v1/items?app_id={AppId}&currency={Currency}";

        using var request = new HttpRequestMessage(HttpMethod.Get, url);

        // Brotli is REQUIRED — Skinport returns 406 Not Acceptable without it.
        request.Headers.TryAddWithoutValidation("Accept-Encoding", "br");

        using var response = await _http.SendAsync(
            request, HttpCompletionOption.ResponseHeadersRead, ct);

        response.EnsureSuccessStatusCode();

        // Decompress Brotli manually — HttpClient doesn't handle br automatically
        // unless configured with HttpClientHandler.AutomaticDecompression, which
        // doesn't include Brotli in .NET's default handler.
        await using var compressed   = await response.Content.ReadAsStreamAsync(ct);
        await using var decompressed = new BrotliStream(compressed, CompressionMode.Decompress);

        return await JsonSerializer.DeserializeAsync<List<SkinportItemDto>>(
            decompressed,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true },
            ct);
    }

    // -------------------------------------------------------------------------
    // Price expansion
    // -------------------------------------------------------------------------

    /// <summary>
    /// Expands one Skinport item into individual SkinPrice records per non-null price field.
    ///
    /// Price type mapping:
    ///   min_price       → LowestListing  (cheapest active listing)
    ///   suggested_price → SuggestedAvg   (Skinport's smoothed reference value)
    ///   mean_price      → Avg7d          (mean of active listings; closest type available)
    ///   median_price    → Median7d       (median of active listings; closest type available)
    ///
    /// Note: mean_price and median_price reflect active listings at fetch time,
    /// not a true 7-day rolling average. The mapping is pragmatic — if dedicated
    /// price types are added later (e.g. ListingMean, ListingMedian), update here.
    ///
    /// VariantId = Guid.Empty — FetchResultPersister resolves it via slug map.
    /// </summary>
    private static IEnumerable<SkinPrice> ExpandPrices(
        string slug,
        string currency,
        SkinportItemDto item,
        DateTime recordedAt)
    {
        if (item.MinPrice       is { } lowest)    yield return Make(slug, currency, PriceTypes.LowestListing, lowest,    item.Quantity, recordedAt);
        if (item.SuggestedPrice is { } suggested) yield return Make(slug, currency, PriceTypes.SuggestedAvg,  suggested, null,          recordedAt);
        if (item.MeanPrice      is { } mean)      yield return Make(slug, currency, PriceTypes.Avg7d,         mean,      null,          recordedAt);
        if (item.MedianPrice    is { } median)     yield return Make(slug, currency, PriceTypes.Median7d,     median,    null,          recordedAt);
    }

    private static SkinPrice Make(
        string slug, string currency, string priceType,
        decimal price, int? volume, DateTime recordedAt) => new()
    {
        VariantId  = Guid.Empty,
        GameId     = "cs2",
        Slug       = slug,
        Source     = Sources.Skinport,
        PriceType  = priceType,
        Price      = price,
        Currency   = currency,
        Volume     = volume,
        RecordedAt = recordedAt,
    };
}