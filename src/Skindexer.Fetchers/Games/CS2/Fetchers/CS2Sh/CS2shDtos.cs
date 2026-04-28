using System.Text.Json.Serialization;

namespace Skindexer.Fetchers.Games.CS2.Fetchers.CS2Sh;

internal sealed class CS2ShResponse
{
    [JsonPropertyName("items")]
    public Dictionary<string, CS2ShItemData> Items { get; init; } = [];
}

internal class CS2ShItemData
{
    [JsonPropertyName("buff")]
    public CS2ShMarketData? Buff { get; init; }

    [JsonPropertyName("youpin")]
    public CS2ShMarketData? Youpin { get; init; }

    [JsonPropertyName("csfloat")]
    public CS2ShMarketData? CsFloat { get; init; }

    [JsonPropertyName("steam")]
    public CS2ShMarketData? Steam { get; init; }

    [JsonPropertyName("skinport")]
    public CS2ShMarketData? Skinport { get; init; }

    [JsonPropertyName("c5game")]
    public CS2ShMarketData? C5Game { get; init; }

    [JsonPropertyName("variants")]
    public Dictionary<string, CS2ShVariantData>? Variants { get; init; }
}

internal sealed class CS2ShVariantData : CS2ShItemData
{
    [JsonPropertyName("display_name")]
    public string DisplayName { get; init; } = string.Empty;

    [JsonPropertyName("version")]
    public string Version { get; init; } = string.Empty;
}

internal sealed class CS2ShMarketData
{
    [JsonPropertyName("ask")]
    public decimal? Ask { get; init; }

    [JsonPropertyName("ask_volume")]
    public int? AskVolume { get; init; }

    [JsonPropertyName("bid")]
    public decimal? Bid { get; init; }

    [JsonPropertyName("bid_volume")]
    public int? BidVolume { get; init; }
}