namespace Skindexer.Contracts.Constants;

public static class PriceTypes
{
    public const string MedianDaily = "median_daily";
    public const string LowestListing = "lowest_listing";
    public const string Avg7d  = "avg_7d";
    public const string Avg30d = "avg_30d";
    public const string Median7d = "median_7d";
    public const string Median30d = "median_30d";
    
    /// <summary>
    /// Most recent actual sale price on Steam Market.
    /// Maps to SteamWebApi `pricelatestsell` field.
    /// </summary>
    public const string LastSold = "last_sold";

    /// <summary>
    /// Highest active Steam buy order price.
    /// Maps to SteamWebApi `buyorderprice` field.
    /// </summary>
    public const string BuyOrder = "buy_order";

    /// <summary>
    /// Lowest price available across third-party markets.
    /// Maps to SteamWebApi `pricereal` field.
    /// </summary>
    public const string LowestMarket = "lowest_market";
    
    /// <summary>
    /// SteamAnalyst safe_price_raw — fallback price used when ongoing price
    /// manipulation is detected on Steam Market. Replaces Avg7d for that item.
    /// </summary>
    public const string SafePrice = "safe_price";
 
    /// <summary>
    /// SteamAnalyst suggested_amount_avg_raw — community-suggested average price
    /// for rare items (knives, gloves) where Steam Market data is unreliable.
    /// </summary>
    public const string SuggestedAvg = "suggested_avg";
 
    /// <summary>
    /// SteamAnalyst suggested_amount_min_raw — lower bound of community-suggested
    /// price range. Represents low-tier patterns/phases for rare items.
    /// </summary>
    public const string SuggestedMin = "suggested_min";
 
    /// <summary>
    /// SteamAnalyst suggested_amount_max_raw — upper bound of community-suggested
    /// price range. Represents high-tier patterns/phases for rare items.
    /// </summary>
    public const string SuggestedMax = "suggested_max";


}