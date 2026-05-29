namespace Skindexer.Contracts.Constants;
public static class Sources
{
    // -------------------------------------------------------------------------
    // CS2
    // -------------------------------------------------------------------------

    /// <summary>Kaggle Steam historical price dataset (CSV import).</summary>
    public const string KaggleSteam = "kaggle-steam";
    
    /// <summary>SteamWebApi — bulk item + price data including buy orders and third-party market prices.</summary>
    public const string SteamWebApi = "steamwebapi";
    
    /// <summary>Pricempire aggregated data — top-level source label used on FetchResult.</summary>
    public const string Pricempire = "pricempire";

    // Per-marketplace sources from Pricempire.
    // Format: "pricempire-{provider_key}" to avoid collisions with other fetchers.
    public const string PricempireBuff163   = "pricempire-buff163";
    public const string PricempireSteam     = "pricempire-steam";
    public const string PricempireDMarket   = "pricempire-dmarket";
    public const string PricempireSkinport  = "pricempire-skinport";
    public const string PricempireSkinbaron = "pricempire-skinbaron";
    public const string PricempireCSFloat   = "pricempire-csfloat";
    public const string PricempireWaxpeer   = "pricempire-waxpeer";
    
    /// <summary>cs2.sh — top-level source label used on FetchResult.</summary>
    public const string CS2Sh = "cs2sh";

    // Per-marketplace sources from cs2.sh.
    // Format: "cs2sh-{marketplace}" to avoid collisions with other fetchers.
    public const string CS2ShBuff     = "cs2sh-buff";
    public const string CS2ShYoupin   = "cs2sh-youpin";
    public const string CS2ShCsFloat  = "cs2sh-csfloat";
    public const string CS2ShSteam    = "cs2sh-steam";
    public const string CS2ShSkinport = "cs2sh-skinport";
    public const string CS2ShC5Game   = "cs2sh-c5game";
    
    /// <summary>Skinport — direct market price feed.</summary>
    public const string Skinport = "skinport";
    
    /// <summary>SteamAnalyst — Steam Market prices with manipulation detection.</summary>
    public const string SteamAnalyst = "steamanalyst";

    
    // -------------------------------------------------------------------------
    // Display names — null until explicitly filled in.
    // When ready: return a string instead of null for that source.
    // -------------------------------------------------------------------------
    public static string? GetDisplayName(string sourceId) => sourceId switch
    {
        KaggleSteam         => null,
        
        SteamWebApi         => null,
        
        Pricempire          => null,
        
        PricempireBuff163   => null,
        PricempireSteam     => null,
        PricempireDMarket   => null,
        PricempireSkinport  => null,
        PricempireSkinbaron => null,
        PricempireCSFloat   => null,
        PricempireWaxpeer   => null,
        
        CS2Sh               => null,
        CS2ShBuff           => null,
        CS2ShYoupin         => null,
        CS2ShCsFloat        => null,
        CS2ShSteam          => null,
        CS2ShSkinport       => null,
        CS2ShC5Game         => null,
        
        Skinport => null,
        
        SteamAnalyst        => null,
        _                   => null
    };
}