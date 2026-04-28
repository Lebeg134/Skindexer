using System.Text.Json.Serialization;

namespace Skindexer.Fetchers.Games.CS2.Fetchers.SteamAnalyst;

/// <summary>
/// A single item entry from GET /v2/{API_KEY}.
///
/// SteamAnalyst returns all items in one flat array. The shape varies
/// by item category — regular items, rare items (>$400), and price-manipulated
/// items each have different fields present/absent. All fields are nullable
/// to handle this gracefully.
///
/// Note: "market_name" is SteamAnalyst's name for the Steam market_hash_name.
/// </summary>
internal sealed class SteamAnalystItemDto
{
    [JsonPropertyName("market_name")]
    public string MarketName { get; init; } = default!;

    // --- Regular items (under ~$400) ---

    /// <summary>7-day average price. Primary price field for normal items.
    /// Absent when ongoing_price_manipulation = "1".</summary>
    [JsonPropertyName("avg_price_7_days_raw")]
    public decimal? AvgPrice7DaysRaw { get; init; }

    /// <summary>30-day average price.</summary>
    [JsonPropertyName("avg_price_30_days_raw")]
    public decimal? AvgPrice30DaysRaw { get; init; }

    // --- Rare items (over ~$400) ---

    /// <summary>Community-suggested average for rare items (knives, gloves, etc.).</summary>
    [JsonPropertyName("suggested_amount_avg_raw")]
    public decimal? SuggestedAmountAvgRaw { get; init; }

    [JsonPropertyName("suggested_amount_min_raw")]
    public decimal? SuggestedAmountMinRaw { get; init; }

    [JsonPropertyName("suggested_amount_max_raw")]
    public decimal? SuggestedAmountMaxRaw { get; init; }

    // --- Price manipulation ---

    /// <summary>"0" or "1". When "1", avg_price_7_days is absent and safe_price is used instead.</summary>
    [JsonPropertyName("ongoing_price_manipulation")]
    public string? OngoingPriceManipulation { get; init; }

    /// <summary>Safe fallback price when manipulation is detected.</summary>
    [JsonPropertyName("safe_price_raw")]
    public decimal? SafePriceRaw { get; init; }

    // --- Volume ---

    [JsonPropertyName("sold_last_24h")]
    public int? SoldLast24h { get; init; }

    [JsonPropertyName("sold_last_7d")]
    public int? SoldLast7d { get; init; }

    // --- Metadata ---

    /// <summary>SteamAnalyst item page URL — stored in Metadata, not mapped to a price field.</summary>
    [JsonPropertyName("link")]
    public string? Link { get; init; }

    /// <summary>Rarity string e.g. "Covert Rifle", "★ Covert Knife".</summary>
    [JsonPropertyName("rarity")]
    public string? Rarity { get; init; }

    // --- Dopplers ---

    /// <summary>
    /// Per-phase prices for Doppler and Gamma Doppler knives.
    /// Not mapped to SkinPrice rows — Doppler phase→paint-index resolution
    /// is a known gap. See GitHub issue.
    /// </summary>
    [JsonPropertyName("phases")]
    public object? Phases { get; init; }

    // Helpers

    public bool IsManipulated =>
        OngoingPriceManipulation == "1";
}