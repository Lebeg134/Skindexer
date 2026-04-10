namespace Skindexer.Fetchers.Games.CS2;

public static class CS2WearHelper
{
    public static readonly IReadOnlyDictionary<string, string> WearSlugs = new Dictionary<string, string>
    {
        ["Factory New"]    = "factory-new",
        ["Minimal Wear"]   = "minimal-wear",
        ["Field-Tested"]   = "field-tested",
        ["Well-Worn"]      = "well-worn",
        ["Battle-Scarred"] = "battle-scarred",
    };

    /// <summary>
    /// Derives wear and StatTrak flag from a Steam market_hash_name.
    /// e.g. "AK-47 | Redline (Field-Tested)"            → wear: Field-Tested, statTrak: false, souvenir: false
    ///      "StatTrak™ AK-47 | Redline (Minimal Wear)"  → wear: Minimal Wear, statTrak: true,  souvenir: false
    ///      "Souvenir AWP | Dragon Lore (Factory New)"  → wear: Factory New,  statTrak: false, souvenir: true
    /// Returns null wear if the name does not contain a wear suffix.
    /// </summary>
    public static (string? Wear, bool StatTrak, bool souvenir) ParseMarketHashName(string marketHashName)
    {
        var statTrak = marketHashName.StartsWith("StatTrak™", StringComparison.OrdinalIgnoreCase);
        var souvenir = marketHashName.StartsWith("Souvenir", StringComparison.OrdinalIgnoreCase);

        var wear = WearSlugs.Keys.FirstOrDefault(w =>
            marketHashName.Contains($"({w})", StringComparison.OrdinalIgnoreCase));

        return (wear, statTrak, souvenir);
    }
}
