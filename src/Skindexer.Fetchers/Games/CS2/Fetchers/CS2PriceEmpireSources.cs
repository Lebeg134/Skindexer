using Skindexer.Contracts.Constants;

namespace Skindexer.Fetchers.Games.CS2.Fetchers;

/// <summary>
/// Maps raw Pricempire provider_key strings to canonical Source constants
/// defined in Skindexer.Contracts.Constants.Sources.
///
/// Kept in Fetchers (not Contracts) because the mapping logic is an
/// implementation detail of this fetcher — Contracts just owns the strings.
/// </summary>
internal static class PricempireSources
{
    public static string FromProviderKey(string providerKey) => providerKey switch
    {
        "buff163"   => Sources.PricempireBuff163,
        "steam"     => Sources.PricempireSteam,
        "dmarket"   => Sources.PricempireDMarket,
        "skinport"  => Sources.PricempireSkinport,
        "skinbaron" => Sources.PricempireSkinbaron,
        "csfloat"   => Sources.PricempireCSFloat,
        "waxpeer"   => Sources.PricempireWaxpeer,
        // Unknown future providers get auto-prefixed rather than dropped.
        // Data is stored; the source string can be modeled later.
        _           => $"pricempire-{providerKey}",
    };
}