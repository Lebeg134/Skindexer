using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Skindexer.Contracts.Constants;
using Skindexer.Contracts.Models;
using Skindexer.Fetchers.Games.CS2.SlugHelpers;
using Skindexer.Fetchers.Interfaces;

namespace Skindexer.Fetchers.Games.CS2.Fetchers.SteamAnalyst;

/// <summary>
/// Fetches CS2 skin prices from SteamAnalyst's bulk endpoint.
/// Implements IScheduledFetcher — runs daily on the FetchScheduler cycle.
///
/// Endpoint: GET https://api.steamanalyst.com/v2/{API_KEY}
/// Returns all items in a single flat JSON array. No pagination.
///
/// Auth: API key embedded in URL path (SteamAnalyst convention).
/// Config key: "SteamAnalyst:ApiKey"
///
/// Prices-only fetcher: Items and Variants are always empty.
/// Item catalog comes from CS2ByMykelItemFetcher.
///
/// Known gaps:
/// - Doppler phase prices not mapped (phase→paint-index resolution pending).
///   Base Doppler slugs will not resolve in FetchResultPersister — warned and dropped.
/// - current_price field intentionally skipped per SteamAnalyst docs recommendation.
/// </summary>
public sealed class CS2SteamAnalystFetcher : IScheduledFetcher
{
    // -------------------------------------------------------------------------
    // IGameFetcher / IScheduledFetcher
    // -------------------------------------------------------------------------
    
    public static readonly FetcherDescriptor Descriptor = new()
    {
        FetcherId = "cs2-steamanalyst",
        Register = (services, _) =>
        {
            services.AddHttpClient<CS2SteamAnalystFetcher>();
            services.AddSingleton<IGameFetcher, CS2SteamAnalystFetcher>();
        }
    };

    public string FetcherId     => Descriptor.FetcherId;
    public string DisplayName   => "CS2 SteamAnalyst Price Fetcher";
    
    public bool IsAuthoritativeItemSource { get; } = false;
    public TimeSpan PollingInterval => TimeSpan.FromHours(24);

    // -------------------------------------------------------------------------
    // Constants
    // -------------------------------------------------------------------------

    private const string BaseUrl = "https://api.steamanalyst.com";

    // -------------------------------------------------------------------------
    // Dependencies
    // -------------------------------------------------------------------------

    private readonly HttpClient _http;
    private readonly ILogger<CS2SteamAnalystFetcher> _logger;
    private readonly string _apiKey;

    public CS2SteamAnalystFetcher(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<CS2SteamAnalystFetcher> logger)
    {
        _http   = httpClientFactory.CreateClient(nameof(CS2SteamAnalystFetcher));
        _logger = logger;
        _apiKey = configuration["SteamAnalyst:ApiKey"]
            ?? throw new InvalidOperationException(
                "Missing configuration key 'SteamAnalyst:ApiKey'. " +
                "Add it to appsettings.json or user secrets.");
    }

    // -------------------------------------------------------------------------
    // IGameFetcher
    // -------------------------------------------------------------------------

    public async Task<FetchResult> FetchAsync(CancellationToken cancellationToken = default)
    {
        var warnings = new List<string>();

        // --- 1. HTTP fetch ---
        List<SteamAnalystItemDto>? rawItems;
        try
        {
            rawItems = await FetchRawAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{Fetcher}] HTTP fetch failed", FetcherId);
            return new FetchResult
            {
                GameId       = "cs2",
                Source       = Sources.SteamAnalyst,
                IsSuccess    = false,
                ErrorMessage = ex.Message,
            };
        }

        if (rawItems is null || rawItems.Count == 0)
        {
            const string msg = "Response was empty or null";
            _logger.LogWarning("[{Fetcher}] {Message}", FetcherId, msg);
            return new FetchResult
            {
                GameId       = "cs2",
                Source       = Sources.SteamAnalyst,
                IsSuccess    = false,
                ErrorMessage = msg,
            };
        }

        _logger.LogInformation(
            "[{Fetcher}] Received {Count} items from SteamAnalyst API",
            FetcherId, rawItems.Count);

        // --- 2. Map to SkinPrice records ---
        var prices        = new List<SkinPrice>();
        int skippedNoWear = 0;
        int manipulated   = 0;
        var recordedAt    = DateTime.UtcNow;

        foreach (var item in rawItems)
        {
            if (!CS2MarketHashNameParser.TryParse(
                    item.MarketName,
                    out var weapon,
                    out var skinName,
                    out var wear,
                    out var isStatTrak,
                    out var isSouvenir))
            {
                // Agents, keys, stickers, music kits, bare Doppler knives, etc.
                skippedNoWear++;
                continue;
            }

            var slug = CS2SlugBuilder.BuildVariantSlug(
                weapon, skinName, wear, isStatTrak, isSouvenir);

            if (item.IsManipulated)
            {
                manipulated++;
                // avg_price_7_days is absent when manipulated — use safe_price instead
                if (item.SafePriceRaw is { } safePrice)
                    prices.Add(Make(slug, PriceTypes.SafePrice, safePrice, null, recordedAt));

                continue;
            }

            prices.AddRange(ExpandPrices(slug, item, recordedAt));
        }

        _logger.LogInformation(
            "[{Fetcher}] Mapped {PriceCount} price records. " +
            "Skipped {SkippedNoWear} non-wear items, {Manipulated} manipulation-flagged items used safe_price.",
            FetcherId, prices.Count, skippedNoWear, manipulated);

        return new FetchResult
        {
            GameId    = "cs2",
            Source    = Sources.SteamAnalyst,
            Items     = [],
            Variants  = [],
            Prices    = prices,
            IsSuccess = true,
            Warnings  = warnings,
        };
    }

    // -------------------------------------------------------------------------
    // HTTP
    // -------------------------------------------------------------------------

    private async Task<List<SteamAnalystItemDto>?> FetchRawAsync(CancellationToken ct)
    {
        var url = $"{BaseUrl}/v2/{_apiKey}";

        using var response = await _http.GetAsync(url, ct);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<List<SteamAnalystItemDto>>(
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                NumberHandling = JsonNumberHandling.AllowReadingFromString,  // ← add this
            },
            ct);
    }

    // -------------------------------------------------------------------------
    // Price expansion
    // -------------------------------------------------------------------------

    /// <summary>
    /// Expands one item DTO into SkinPrice rows for each non-null price field.
    /// Regular items produce avg_7d / avg_30d rows.
    /// Rare items (suggested amounts) produce suggested_avg / suggested_min / suggested_max rows.
    /// Items can have both sets if SteamAnalyst returns both — each non-null field gets its own row.
    /// VariantId = Guid.Empty — resolved by FetchResultPersister via slug map.
    /// </summary>
    private static IEnumerable<SkinPrice> ExpandPrices(
        string slug,
        SteamAnalystItemDto item,
        DateTime recordedAt)
    {
        // Regular item prices
        if (item.AvgPrice7DaysRaw  is { } avg7)  yield return Make(slug, PriceTypes.Avg7d,                   avg7,  item.SoldLast7d,  recordedAt);
        if (item.AvgPrice30DaysRaw is { } avg30)  yield return Make(slug, PriceTypes.Avg30d,                  avg30, null,            recordedAt);

        // Rare item prices
        if (item.SuggestedAmountAvgRaw is { } sugAvg) yield return Make(slug, PriceTypes.SuggestedAvg, sugAvg, null, recordedAt);
        if (item.SuggestedAmountMinRaw is { } sugMin) yield return Make(slug, PriceTypes.SuggestedMin, sugMin, null, recordedAt);
        if (item.SuggestedAmountMaxRaw is { } sugMax) yield return Make(slug, PriceTypes.SuggestedMax, sugMax, null, recordedAt);
    }

    private static SkinPrice Make(
        string slug,
        string priceType,
        decimal price,
        int? volume,
        DateTime recordedAt) => new()
    {
        VariantId  = Guid.Empty,
        GameId     = "cs2",
        Slug       = slug,
        Source     = Sources.SteamAnalyst,
        PriceType  = priceType,
        Price      = price,
        Currency   = "USD",
        Volume     = volume,
        RecordedAt = recordedAt,
    };
}