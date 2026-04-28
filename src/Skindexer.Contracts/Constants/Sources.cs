namespace Skindexer.Contracts.Constants;
public static class Sources
{
    // -------------------------------------------------------------------------
    // CS2
    // -------------------------------------------------------------------------

    /// <summary>Kaggle Steam historical price dataset (CSV import).</summary>
    public const string KaggleSteam = "kaggle-steam";

    /// <summary>Pricempire aggregated data — top-level source label used on FetchResult.</summary>
    public const string Pricempire = "pricempire";
    
    /// <summary>SteamWebApi — bulk item + price data including buy orders and third-party market prices.</summary>
    public const string SteamWebApi = "steamwebapi";

    // Per-marketplace sources from Pricempire.
    // Format: "pricempire-{provider_key}" to avoid collisions with other fetchers.
    public const string PricempireBuff163   = "pricempire-buff163";
    public const string PricempireSteam     = "pricempire-steam";
    public const string PricempireDMarket   = "pricempire-dmarket";
    public const string PricempireSkinport  = "pricempire-skinport";
    public const string PricempireSkinbaron = "pricempire-skinbaron";
    public const string PricempireCSFloat   = "pricempire-csfloat";
    public const string PricempireWaxpeer   = "pricempire-waxpeer";
    
    /// <summary>SteamAnalyst — Steam Market prices with manipulation detection.</summary>
    public const string SteamAnalyst = "steamanalyst";

}