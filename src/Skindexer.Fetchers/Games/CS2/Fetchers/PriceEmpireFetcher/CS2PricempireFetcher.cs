using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Skindexer.Contracts.Constants;
using Skindexer.Contracts.Models;
using Skindexer.Fetchers.Games.CS2.SlugHelpers;
using Skindexer.Fetchers.Interfaces;

namespace Skindexer.Fetchers.Games.CS2.Fetchers.PriceEmpireFetcher;

/// <summary>
/// Fetches live CS2 skin prices from Pricempire's bulk endpoint.
/// </summary>
public sealed class CS2PricempireFetcher : IScheduledFetcher
{
    public static readonly FetcherDescriptor Descriptor = new()
    {
        FetcherId = "cs2-pricempire",
        Register = (services, _) =>
        {
            services.AddHttpClient<CS2PricempireFetcher>();
            services.AddSingleton<IGameFetcher, CS2PricempireFetcher>();
        }
    };

    private const string GameId = GameIds.CounterStrike;
    private const string BaseUrl = "https://api.pricempire.com";
    private const int AppId = 730;
    private const string Currency = "USD";

    private static readonly string[] DefaultSources =
    [
        "buff163",
        "steam",
        "dmarket",
        "skinport",
        "skinbaron",
        "csfloat",
        "waxpeer",
    ];

    private readonly HttpClient _http;
    private readonly ILogger<CS2PricempireFetcher> _logger;
    private readonly string _apiKey;

    public CS2PricempireFetcher(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<CS2PricempireFetcher> logger)
    {
        _http = httpClientFactory.CreateClient(nameof(CS2PricempireFetcher));
        _logger = logger;
        _apiKey = configuration["Pricempire:ApiKey"]
            ?? throw new InvalidOperationException(
                "Missing configuration key 'Pricempire:ApiKey'. " +
                "Add it to appsettings.json or user secrets.");
    }

    public string FetcherId => Descriptor.FetcherId;
    public string DisplayName => "CS2 Pricempire Price Fetcher";
    public string DefaultCronExpression => "0 1 * * *"; // 1:00 AM daily
    public bool IsAuthoritativeItemSource => false;

    // --- Core Execution ---

    public async Task<FetchResult> FetchAsync(CancellationToken cancellationToken = default)
    {
        var warnings = new List<string>();

        List<PricempireItemDto>? rawItems;
        try
        {
            rawItems = await FetchRawAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{Fetcher}] HTTP fetch failed", FetcherId);
            return new FetchResult
            {
                GameId = GameId,
                Source = Sources.Pricempire,
                IsSuccess = false,
                ErrorMessage = ex.Message,
            };
        }

        if (rawItems is null || rawItems.Count == 0)
        {
            const string msg = "Response was empty or null";
            _logger.LogWarning("[{Fetcher}] {Message}", FetcherId, msg);
            return new FetchResult
            {
                GameId = GameId,
                Source = Sources.Pricempire,
                IsSuccess = false,
                ErrorMessage = msg,
            };
        }

        _logger.LogInformation(
            "[{Fetcher}] Received {Count} items from Pricempire API",
            FetcherId, rawItems.Count);

        var prices = new List<SkinPrice>();
        int skipped = 0;
        int skippedNoWear = 0;
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

            foreach (var priceEntry in item.Prices)
            {
                if (string.IsNullOrWhiteSpace(priceEntry.ProviderKey))
                    continue;

                var source = PricempireSources.FromProviderKey(priceEntry.ProviderKey);
                prices.AddRange(ExpandPrices(slug, source, priceEntry, recordedAt));
            }
        }

        if (skipped > 0)
            warnings.Add($"{skipped} items skipped due to unresolvable market_hash_name");

        _logger.LogInformation(
            "[{Fetcher}] Mapped {PriceCount} price records. " +
            "Skipped {SkippedNoWear} non-wear items (agents/keys/etc), {Skipped} unresolvable.",
            FetcherId, prices.Count, skippedNoWear, skipped);

        return new FetchResult
        {
            GameId = GameId,
            Source = Sources.Pricempire,
            Items = [],
            Variants = [],
            Prices = prices,
            IsSuccess = true,
            Warnings = warnings,
        };
    }

    // --- External Integrations & Price Layout Mappers ---

    private async Task<List<PricempireItemDto>?> FetchRawAsync(CancellationToken ct)
    {
        var sources = Uri.EscapeDataString(string.Join(",", DefaultSources));
        var url = $"{BaseUrl}/v4/paid/items/prices?app_id={AppId}&sources={sources}&currency={Currency}";

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _apiKey);

        using var response = await _http.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<List<PricempireItemDto>>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true },
            ct);
    }

    private static IEnumerable<SkinPrice> ExpandPrices(
        string slug,
        string source,
        PricempirePriceEntryDto entry,
        DateTime recordedAt)
    {
        var ts = entry.UpdatedAt ?? recordedAt;

        if (entry.Price is { } lowest) yield return Make(slug, source, PriceTypes.LowestListing, lowest, entry.Count, ts);
        if (entry.Avg7 is { } avg7) yield return Make(slug, source, PriceTypes.Avg7d, avg7, null, ts);
        if (entry.Avg30 is { } avg30) yield return Make(slug, source, PriceTypes.Avg30d, avg30, null, ts);
        if (entry.Median7 is { } med7) yield return Make(slug, source, PriceTypes.Median7d, med7, null, ts);
        if (entry.Median30 is { } med30) yield return Make(slug, source, PriceTypes.Median30d, med30, null, ts);
    }

    private static SkinPrice Make(
        string slug, 
        string source, 
        string priceType,
        decimal price, 
        int? volume, 
        DateTime recordedAt) => new()
    {
        VariantId = Guid.Empty,
        GameId = GameId,
        Slug = slug,
        Source = source,
        PriceType = priceType,
        Price = price,
        Currency = "USD",
        Volume = volume,
        RecordedAt = recordedAt,
    };
}