using System.Text.Json.Serialization;

namespace Skindexer.Fetchers.Games.CS2.Fetchers.SteamWebApiFetcher;

internal sealed class SteamWebApiItemDto
{
    [JsonPropertyName("markethashname")]
    public string? MarketHashName { get; init; }

    [JsonPropertyName("pricelatest")]
    public decimal? PriceLatest { get; init; }

    [JsonPropertyName("pricelatestsell")]
    public decimal? PriceLatestSell { get; init; }

    [JsonPropertyName("buyorderprice")]
    public decimal? BuyOrderPrice { get; init; }

    [JsonPropertyName("pricereal")]
    public decimal? PriceReal { get; init; }

    [JsonPropertyName("sold24h")]
    public int? Sold24h { get; init; }

    [JsonPropertyName("image")]
    public string? Image { get; init; }

    [JsonPropertyName("rarity")]
    public string? Rarity { get; init; }
}