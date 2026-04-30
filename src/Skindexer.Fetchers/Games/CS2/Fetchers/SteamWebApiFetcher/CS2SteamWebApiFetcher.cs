using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Skindexer.Contracts.Constants;
using Skindexer.Contracts.Models;
using Skindexer.Fetchers.Games.CS2.SlugHelpers;
using Skindexer.Fetchers.Interfaces;

namespace Skindexer.Fetchers.Games.CS2.Fetchers.SteamWebApiFetcher;

/// <summary>
/// To find out more details about the SteamWebAPI visit:
/// https://www.steamwebapi.com/
/// </summary>
public sealed class CS2SteamWebApiFetcher : IScheduledFetcher
{
    public static readonly FetcherDescriptor Descriptor = new()
    {
        FetcherId = "cs2-steamwebapi",
        Register = (services, _) =>
        {
            services.AddHttpClient<CS2SteamWebApiFetcher>();
            services.AddSingleton<IGameFetcher, CS2SteamWebApiFetcher>();
        }
    };
    
    private const string GameId = GameIds.CounterStrike;
    private const string BaseUrl = "https://www.steamwebapi.com/steam/api/items";

    private const string SelectFields =
        "markethashname,pricelatest,pricelatestsell,buyorderprice,pricereal,sold24h,image,rarity";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<CS2SteamWebApiFetcher> _logger;
    private readonly string _apiKey;

    public string FetcherId => Descriptor.FetcherId;
    public string DisplayName => "CS2 SteamWebApi";

    public bool IsAuthoritativeItemSource { get; } = false;
    public TimeSpan PollingInterval => TimeSpan.FromHours(6);

    public CS2SteamWebApiFetcher(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<CS2SteamWebApiFetcher> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _apiKey = configuration["SteamWebApi:ApiKey"]
                  ?? throw new InvalidOperationException("SteamWebApi:ApiKey is not configured.");
    }

    public async Task<FetchResult> FetchAsync(CancellationToken cancellationToken = default)
    {
        List<SteamWebApiItemDto> raw;

        try
        {
            var client = _httpClientFactory.CreateClient(nameof(CS2SteamWebApiFetcher));
            var url = $"{BaseUrl}?key={_apiKey}&game=cs2&select={SelectFields}&production=1&max=50000";

            var response = await client.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            raw = await response.Content.ReadFromJsonAsync<List<SteamWebApiItemDto>>(
                      cancellationToken: cancellationToken)
                  ?? [];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch data from SteamWebApi");
            return FetchResult.Failure(GameId, Sources.SteamWebApi, ex.Message);
        }

        return MapResults(raw);
    }


    private FetchResult MapResults(List<SteamWebApiItemDto> raw)
    {
        var items = new List<SkinItem>();
        var variants = new List<SkinVariant>();
        var prices = new List<SkinPrice>();
        var warnings = new List<string>();

        // Deduplicate items by base slug — multiple variants share the same SkinItem
        var itemsBySlug = new Dictionary<string, SkinItem>(StringComparer.Ordinal);

        foreach (var dto in raw)
        {
            if (string.IsNullOrWhiteSpace(dto.MarketHashName))
                continue;

            if (!CS2MarketHashNameParser.TryParse(
                    dto.MarketHashName,
                    out var weapon,
                    out var skinName,
                    out var wear,
                    out var statTrak,
                    out var souvenir))
            {
                // Dopplers with phase info and non-weapon items (knives without wear, gloves, agents, etc.)
                // are silently skipped here. Doppler phase handling is a known gap — see GitHub issue.
                continue;
            }

            var baseSlug = CS2SlugBuilder.BuildBaseSlug(weapon, skinName);
            var variantSlug = CS2SlugBuilder.BuildVariantSlug(weapon, skinName, wear, statTrak, souvenir);
            var now = DateTime.UtcNow;

            // Upsert SkinItem — one per base slug
            if (!itemsBySlug.TryGetValue(baseSlug, out var item))
            {
                item = new SkinItem
                {
                    Id = Guid.NewGuid(),
                    GameId = GameId,
                    ItemType = CS2ItemTypes.WeaponSkin,
                    Slug = baseSlug,
                    Name = $"{weapon} | {skinName}",
                    ImageUrl = dto.Image,
                    IsTradeable = true,
                    IsMarketable = true,
                    CreatedAt = now,
                    UpdatedAt = now,
                };

                itemsBySlug[baseSlug] = item;
                items.Add(item);
            }

            var variant = new SkinVariant
            {
                Id = Guid.NewGuid(),
                ItemId = item.Id,
                GameId = GameId,
                Slug = variantSlug,
                Metadata = new Dictionary<string, object?>
                {
                    ["wear"] = wear,
                    ["stattrak"] = statTrak,
                    ["souvenir"] = souvenir,
                    ["rarity"] = dto.Rarity,
                },
            };

            variants.Add(variant);

            // --- Prices ---

            AddPrice(prices, variant, Sources.SteamWebApi, PriceTypes.LowestListing, dto.PriceLatest, volume: null,
                now);
            AddPrice(prices, variant, Sources.SteamWebApi, PriceTypes.LastSold, dto.PriceLatestSell,
                volume: dto.Sold24h, now);
            AddPrice(prices, variant, Sources.SteamWebApi, PriceTypes.BuyOrder, dto.BuyOrderPrice, volume: null, now);
            AddPrice(prices, variant, Sources.SteamWebApi, PriceTypes.LowestMarket, dto.PriceReal, volume: null, now);
        }

        _logger.LogInformation(
            "SteamWebApi fetch complete. Items: {Items}, Variants: {Variants}, Prices: {Prices}, Warnings: {Warnings}",
            items.Count, variants.Count, prices.Count, warnings.Count);

        return warnings.Count > 0
            ? FetchResult.Partial(GameId, Sources.SteamWebApi, items, variants, prices, warnings)
            : FetchResult.Success(GameId, Sources.SteamWebApi, items, variants, prices);
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
            VariantId = variant.Id,
            GameId = GameId,
            Slug = variant.Slug,
            Source = source,
            PriceType = priceType,
            Price = value.Value,
            Currency = "USD",
            Volume = volume,
            RecordedAt = recordedAt,
        });
    }
}