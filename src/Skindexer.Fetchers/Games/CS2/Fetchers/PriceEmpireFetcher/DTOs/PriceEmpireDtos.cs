using System.Text.Json.Serialization;

namespace Skindexer.Fetchers.Games.CS2.Fetchers.PriceEmpireFetcher.DTOs;

/// <summary>
/// Top-level item entry returned by GET /v4/paid/items/prices
/// </summary>
internal sealed class PricempireItemDto
{
    [JsonPropertyName("market_hash_name")]
    public string MarketHashName { get; init; } = default!;

    /// <summary>
    /// Liquidity score 0–100. Higher = more liquid market.
    /// Stored in Metadata for informational purposes, not used for routing.
    /// </summary>
    [JsonPropertyName("liquidity")]
    public int? Liquidity { get; init; }

    /// <summary>Total listings across all providers.</summary>
    [JsonPropertyName("count")]
    public int? Count { get; init; }

    [JsonPropertyName("prices")]
    public List<PricempirePriceEntryDto> Prices { get; init; } = [];
}

/// <summary>
/// Per-provider price block nested inside a PricempireItemDto.
/// A single item may have N of these — one per marketplace.
/// </summary>
internal sealed class PricempirePriceEntryDto
{
    /// <summary>
    /// Marketplace identifier, e.g. "buff163", "dmarket", "steam", "skinport".
    /// Maps to SkinPrice.Source via PricempireSources constants.
    /// </summary>
    [JsonPropertyName("provider_key")]
    public string ProviderKey { get; init; } = default!;

    /// <summary>Current lowest listing price in the requested currency. Maps to PriceTypes.LowestListing.</summary>
    [JsonPropertyName("price")]
    public decimal? Price { get; init; }

    /// <summary>Number of listings for this provider.</summary>
    [JsonPropertyName("count")]
    public int? Count { get; init; }

    [JsonPropertyName("updated_at")]
    public DateTime? UpdatedAt { get; init; }

    // --- Rolling averages (all nullable — not all providers supply them) ---

    [JsonPropertyName("avg_7")]
    public decimal? Avg7 { get; init; }

    [JsonPropertyName("avg_30")]
    public decimal? Avg30 { get; init; }

    [JsonPropertyName("avg_60")]
    public decimal? Avg60 { get; init; }

    [JsonPropertyName("avg_90")]
    public decimal? Avg90 { get; init; }

    [JsonPropertyName("median_7")]
    public decimal? Median7 { get; init; }

    [JsonPropertyName("median_30")]
    public decimal? Median30 { get; init; }

    [JsonPropertyName("median_60")]
    public decimal? Median60 { get; init; }

    [JsonPropertyName("median_90")]
    public decimal? Median90 { get; init; }
}