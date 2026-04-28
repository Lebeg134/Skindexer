namespace Skindexer.Fetchers.Games.CS2.SlugHelpers;

/// <summary>
/// Parses a Steam market_hash_name string into the components needed
/// to build a canonical Skindexer slug via CS2KaggleSlugHelper.BuildPriceSlug.
///
/// Steam market_hash_name format:
///   [{Prefix} ]{Weapon} | {SkinName} ({Wear})
///
/// Where prefix is optionally one of:
///   "StatTrak™ "   → StatTrak = true
///   "Souvenir "    → Souvenir = true
///
/// Examples:
///   "AK-47 | Redline (Field-Tested)"
///       → weapon: "AK-47", skin: "Redline", wear: "Field-Tested", statTrak: false, souvenir: false
///
///   "StatTrak™ AK-47 | Redline (Minimal Wear)"
///       → weapon: "AK-47", skin: "Redline", wear: "Minimal Wear", statTrak: true, souvenir: false
///
///   "Souvenir AK-47 | Redline (Factory New)"
///       → weapon: "AK-47", skin: "Redline", wear: "Factory New", statTrak: false, souvenir: true
///
/// Items without a pipe separator (knives, gloves, agents, keys, etc.)
/// are not parseable as weapon skin variants and return false.
/// </summary>
public static class CS2MarketHashNameParser
{
    private const string StatTrakPrefix  = "StatTrak\u2122 "; // StatTrak™ 
    private const string SouvenirPrefix  = "Souvenir ";

    private static readonly string[] KnownWears =
    [
        "Factory New",
        "Minimal Wear",
        "Field-Tested",
        "Well-Worn",
        "Battle-Scarred",
    ];

    /// <summary>
    /// Attempts to parse a Steam market_hash_name into its component parts.
    /// Returns false for items that are not parseable as weapon skin variants
    /// (no pipe, no wear suffix, etc.) — callers should skip those items.
    /// </summary>
    public static bool TryParse(
        string marketHashName,
        out string weapon,
        out string skinName,
        out string wear,
        out bool statTrak,
        out bool souvenir)
    {
        weapon   = string.Empty;
        skinName = string.Empty;
        wear     = string.Empty;
        statTrak = false;
        souvenir = false;

        if (string.IsNullOrWhiteSpace(marketHashName))
            return false;

        var name = marketHashName;

        // Detect and strip prefix
        if (name.StartsWith(StatTrakPrefix, StringComparison.Ordinal))
        {
            statTrak = true;
            name = name[StatTrakPrefix.Length..];
        }
        else if (name.StartsWith(SouvenirPrefix, StringComparison.Ordinal))
        {
            souvenir = true;
            name = name[SouvenirPrefix.Length..];
        }

        // Must contain a pipe to be a weapon | skin pair
        var pipeIndex = name.IndexOf(" | ", StringComparison.Ordinal);
        if (pipeIndex < 0)
            return false;

        weapon = name[..pipeIndex].Trim();
        var remainder = name[(pipeIndex + 3)..].Trim(); // skip " | "

        // Extract wear from trailing "(Wear)" suffix
        var detectedWear = ExtractWear(remainder, out var skinPart);
        if (detectedWear is null)
            return false;

        skinName = skinPart.Trim();
        wear = detectedWear;
        return true;
    }

    private static string? ExtractWear(string remainder, out string skinPart)
    {
        foreach (var knownWear in KnownWears)
        {
            var suffix = $" ({knownWear})";
            if (remainder.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
            {
                skinPart = remainder[..^suffix.Length];
                return knownWear;
            }
        }

        skinPart = remainder;
        return null;
    }
}