namespace Skindexer.Fetchers.Games.CS2;

public static class CS2SlugHelper
{
    private static readonly Dictionary<string, string> WearSlugs = new()
    {
        ["Factory New"]    = "factory-new",
        ["Minimal Wear"]   = "minimal-wear",
        ["Field-Tested"]   = "field-tested",
        ["Well-Worn"]      = "well-worn",
        ["Battle-Scarred"] = "battle-scarred",
    };

    /// <summary>
    /// Produces a canonical slug for a CS2 skin wear variant.
    /// e.g. "cz75-auto-victoria-field-tested"
    ///      "cz75-auto-victoria-stattrak-field-tested"
    /// </summary>
    public static string Build(string weapon, string skinName, string wear, bool statTrak)
    {
        var wearSlug = WearSlugs.GetValueOrDefault(wear, Slugify(wear));

        var parts = new List<string>
        {
            Slugify(weapon),
            Slugify(skinName),
        };

        if (statTrak)
            parts.Add("stattrak");

        parts.Add(wearSlug);

        return string.Join("-", parts);
    }

    /// <summary>
    /// Derives wear and StatTrak flag from a Steam market_hash_name.
    /// e.g. "AK-47 | Redline (Field-Tested)"           → wear: Field-Tested, statTrak: false
    ///      "StatTrak™ AK-47 | Redline (Minimal Wear)"  → wear: Minimal Wear,  statTrak: true
    /// Returns null wear if the name does not contain a wear suffix.
    /// </summary>
    public static (string? Wear, bool StatTrak) ParseMarketHashName(string marketHashName)
    {
        var statTrak = marketHashName.StartsWith("StatTrak™", StringComparison.OrdinalIgnoreCase);

        var wear = WearSlugs.Keys.FirstOrDefault(w => marketHashName.Contains($"({w})", StringComparison.OrdinalIgnoreCase));

        return (wear, statTrak);
    }

    private static string Slugify(string value) =>
        value
            .ToLowerInvariant()
            .Replace("™", "")
            .Replace("|", "")
            .Replace("'", "")
            .Replace(".", "")
            .Replace(" ", "-")
            .Replace("--", "-")
            .Trim('-');
}