using System.Text.Json.Serialization;

namespace Skindexer.Fetchers.Games.CS2.Fetchers.Skinport;

/// <summary>
/// Represents one item entry returned by GET https://api.skinport.com/v1/items
///
/// All prices are in the requested currency (USD by default).
/// The endpoint returns all active listings aggregated per market_hash_name.
/// </summary>
internal sealed class SkinportItemDto
{
    [JsonPropertyName("market_hash_name")]
    public string MarketHashName { get; init; } = default!;

    [JsonPropertyName("currency")]
    public string Currency { get; init; } = default!;

    /// <summary>
    /// Skinport's own suggested price — a smoothed reference value.
    /// Maps to PriceTypes.SuggestedAvg.
    /// </summary>
    [JsonPropertyName("suggested_price")]
    public decimal? SuggestedPrice { get; init; }

    /// <summary>Lowest active listing price. Maps to PriceTypes.LowestListing.</summary>
    [JsonPropertyName("min_price")]
    public decimal? MinPrice { get; init; }

    /// <summary>Highest active listing price. Stored for reference; no dedicated PriceType yet.</summary>
    [JsonPropertyName("max_price")]
    public decimal? MaxPrice { get; init; }

    /// <summary>Mean price of active listings. Maps to PriceTypes.Avg7d (closest available type).</summary>
    [JsonPropertyName("mean_price")]
    public decimal? MeanPrice { get; init; }

    /// <summary>Median price of active listings. Maps to PriceTypes.Median7d (closest available type).</summary>
    [JsonPropertyName("median_price")]
    public decimal? MedianPrice { get; init; }

    /// <summary>Number of active listings.</summary>
    [JsonPropertyName("quantity")]
    public int? Quantity { get; init; }
}