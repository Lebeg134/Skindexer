using System.IO.Compression;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Skindexer.Contracts.Constants;
using Skindexer.Contracts.Models;
using Skindexer.Fetchers.Games.CS2.SlugHelpers;
using Skindexer.Fetchers.Interfaces;

namespace Skindexer.Fetchers.Games.CS2.Fetchers.CS2Sh;

/// <summary>
/// Yeah, funky name I know, but must keep them consistent!
/// You can find out more about the API on: https://cs2.sh/
/// </summary>
public sealed class CS2CS2ShFetcher : IScheduledFetcher
{
    private const string GameId  = GameIds.CounterStrike;
    private const string BaseUrl = "https://api.cs2.sh/v1/prices/latest";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<CS2CS2ShFetcher> _logger;
    private readonly string _apiKey;

    public string FetcherId      => "cs2-cs2sh";
    public string DisplayName    => "CS2 cs2.sh";
    
    public bool IsAuthoritativeItemSource { get; } = false;
    public TimeSpan PollingInterval => TimeSpan.FromHours(1);

    public CS2CS2ShFetcher(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<CS2CS2ShFetcher> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger            = logger;
        _apiKey            = configuration["CS2Sh:ApiKey"]
                             ?? throw new InvalidOperationException("CS2Sh:ApiKey is not configured.");
    }

    public async Task<FetchResult> FetchAsync(CancellationToken cancellationToken = default)
    {
        CS2ShResponse response;

        try
        {
            var client  = _httpClientFactory.CreateClient(nameof(CS2CS2ShFetcher));
            var request = new HttpRequestMessage(HttpMethod.Get, BaseUrl);
            request.Headers.Add("Authorization", $"Bearer {_apiKey}");
            request.Headers.Add("Accept-Encoding", "gzip");

            var httpResponse = await client.SendAsync(request, cancellationToken);
            httpResponse.EnsureSuccessStatusCode();

            // cs2.sh always returns gzip — decompress manually since we're
            // using SendAsync directly rather than GetFromJsonAsync
            await using var compressed   = await httpResponse.Content.ReadAsStreamAsync(cancellationToken);
            await using var decompressed = new GZipStream(compressed, CompressionMode.Decompress);

            response = await JsonSerializer.DeserializeAsync<CS2ShResponse>(
                           decompressed,
                           cancellationToken: cancellationToken)
                       ?? new CS2ShResponse();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch data from cs2.sh");
            return FetchResult.Failure(GameId, Sources.CS2Sh, ex.Message);
        }

        return MapResults(response);
    }

    private FetchResult MapResults(CS2ShResponse response)
    {
        var items    = new List<SkinItem>();
        var variants = new List<SkinVariant>();
        var prices   = new List<SkinPrice>();
        var warnings = new List<string>();

        var itemsBySlug = new Dictionary<string, SkinItem>(StringComparer.Ordinal);
        var now         = DateTime.UtcNow;

        foreach (var (marketHashName, itemData) in response.Items)
        {
            if (!CS2MarketHashNameParser.TryParse(
                    marketHashName,
                    out var weapon,
                    out var skinName,
                    out var wear,
                    out var statTrak,
                    out var souvenir))
                continue;

            var baseSlug    = CS2SlugBuilder.BuildBaseSlug(weapon, skinName);
            var variantSlug = CS2SlugBuilder.BuildVariantSlug(weapon, skinName, wear, statTrak, souvenir);

            var item = GetOrCreateItem(itemsBySlug, items, baseSlug, weapon, skinName, now);

            // Base variant (no phase)
            var variant = CreateVariant(item, GameId, variantSlug, wear, statTrak, souvenir, phase: null);
            variants.Add(variant);
            AddMarketplacePrices(prices, variant, itemData, now);

            // Doppler / Case Hardened phase variants
            if (itemData.Variants is not null)
            {
                foreach (var (_, phaseData) in itemData.Variants)
                {
                    var phaseSlug    = $"{variantSlug}-{phaseData.Version}";
                    var phaseVariant = CreateVariant(item, GameId, phaseSlug, wear, statTrak, souvenir, phaseData.DisplayName);
                    variants.Add(phaseVariant);
                    AddMarketplacePrices(prices, phaseVariant, phaseData, now);
                }
            }
        }

        _logger.LogInformation(
            "cs2.sh fetch complete. Items: {Items}, Variants: {Variants}, Prices: {Prices}",
            items.Count, variants.Count, prices.Count);

        return warnings.Count > 0
            ? FetchResult.Partial(GameId, Sources.CS2Sh, items, variants, prices, warnings)
            : FetchResult.Success(GameId, Sources.CS2Sh, items, variants, prices);
    }

    // --- Helpers ---

    private static SkinItem GetOrCreateItem(
        Dictionary<string, SkinItem> itemsBySlug,
        List<SkinItem> items,
        string baseSlug,
        string weapon,
        string skinName,
        DateTime now)
    {
        if (itemsBySlug.TryGetValue(baseSlug, out var existing))
            return existing;

        var item = new SkinItem
        {
            Id           = Guid.NewGuid(),
            GameId       = GameId,
            ItemType     = CS2ItemTypes.WeaponSkin,
            Slug         = baseSlug,
            Name         = $"{weapon} | {skinName}",
            IsTradeable  = true,
            IsMarketable = true,
            CreatedAt    = now,
            UpdatedAt    = now,
        };

        itemsBySlug[baseSlug] = item;
        items.Add(item);
        return item;
    }

    private static SkinVariant CreateVariant(
        SkinItem item,
        string gameId,
        string variantSlug,
        string wear,
        bool statTrak,
        bool souvenir,
        string? phase)
    {
        var metadata = new Dictionary<string, object?>
        {
            ["wear"]      = wear,
            ["stattrak"]  = statTrak,
            ["souvenir"]  = souvenir,
        };

        if (phase is not null)
            metadata["phase"] = phase;

        return new SkinVariant
        {
            Id       = Guid.NewGuid(),
            ItemId   = item.Id,
            GameId   = gameId,
            Slug     = variantSlug,
            Metadata = metadata,
        };
    }

    private static void AddMarketplacePrices(
        List<SkinPrice> prices,
        SkinVariant variant,
        CS2ShItemData data,
        DateTime now)
    {
        AddSourcePrices(prices, variant, Sources.CS2ShBuff,     data.Buff,     now);
        AddSourcePrices(prices, variant, Sources.CS2ShYoupin,   data.Youpin,   now);
        AddSourcePrices(prices, variant, Sources.CS2ShCsFloat,  data.CsFloat,  now);
        AddSourcePrices(prices, variant, Sources.CS2ShSteam,    data.Steam,    now);
        AddSourcePrices(prices, variant, Sources.CS2ShSkinport, data.Skinport, now);
        AddSourcePrices(prices, variant, Sources.CS2ShC5Game,   data.C5Game,   now);
    }

    private static void AddSourcePrices(
        List<SkinPrice> prices,
        SkinVariant variant,
        string source,
        CS2ShMarketData? market,
        DateTime now)
    {
        if (market is null)
            return;

        AddPrice(prices, variant, source, PriceTypes.LowestListing, market.Ask,    market.AskVolume, now);
        AddPrice(prices, variant, source, PriceTypes.BuyOrder,       market.Bid,    market.BidVolume, now);
    }

    private static void AddPrice(
        List<SkinPrice> prices,
        SkinVariant variant,
        string source,
        string priceType,
        decimal? value,
        int? volume,
        DateTime recordedAt)
    {
        if (value is null or <= 0)
            return;

        prices.Add(new SkinPrice
        {
            VariantId  = variant.Id,
            GameId     = GameId,
            Slug       = variant.Slug,
            Source     = source,
            PriceType  = priceType,
            Price      = value.Value,
            Currency   = "USD",
            Volume     = volume,
            RecordedAt = recordedAt,
        });
    }
}