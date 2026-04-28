namespace Skindexer.Fetchers.Games.CS2.SlugHelpers;

/// <summary>
/// Single source of truth for building canonical CS2 variant slugs.
///
/// Format: {itemSlug}-[stattrak-][souvenir-]{wearSlug}
///
/// Examples:
///   weapon: "AK-47", skin: "Redline", wear: "Field-Tested", statTrak: false
///       → "ak-47-redline-field-tested"
///
///   weapon: "AK-47", skin: "Redline", wear: "Minimal Wear", statTrak: true
///       → "ak-47-redline-stattrak-minimal-wear"
///
///   itemSlug: "ak-47-redline", wear: "Factory New", souvenir: true
///       → "ak-47-redline-souvenir-factory-new"
///
/// All fetchers (Kaggle, ByMykel, Pricempire, SteamAnalyst) route through
/// here to guarantee slug consistency across import sources.
/// </summary>
public static class CS2SlugBuilder
{
    /// <summary>
    /// Builds a variant slug from raw weapon and skin name strings.
    /// Used by fetchers that receive data as separate fields (Kaggle CSV,
    /// Pricempire/SteamAnalyst via CS2MarketHashNameParser output).
    /// </summary>
    public static string BuildVariantSlug(
        string weapon,
        string skinName,
        string wear,
        bool statTrak,
        bool souvenir)
    {
        var itemSlug = $"{Slugify(weapon)}-{Slugify(skinName)}";
        return BuildVariantSlug(itemSlug, wear, statTrak, souvenir);
    }

    /// <summary>
    /// Builds a variant slug from a pre-built item slug.
    /// Used by fetchers that already have the item slug (ByMykel).
    /// </summary>
    public static string BuildVariantSlug(
        string itemSlug,
        string wear,
        bool statTrak,
        bool souvenir)
    {
        var wearSlug = CS2WearHelper.WearSlugs.GetValueOrDefault(wear, Slugify(wear));

        var parts = new List<string> { itemSlug };

        if (statTrak)
            parts.Add("stattrak");

        if (souvenir)
            parts.Add("souvenir");

        parts.Add(wearSlug);

        return string.Join("-", parts);
    }

    /// <summary>
    /// Builds the base item slug (no wear, no prefix) from weapon and skin name.
    /// e.g. weapon: "AK-47", skinName: "Redline" → "ak-47-redline"
    /// </summary>
    public static string BuildBaseSlug(string weapon, string skinName) =>
        $"{Slugify(weapon)}-{Slugify(skinName)}";

    internal static string Slugify(string value) =>
        value
            .ToLowerInvariant()
            .Replace("★", "")
            .Replace("™", "")
            .Replace("|", "")
            .Replace("'", "")
            .Replace(".", "")
            .Replace("(", "")
            .Replace(")", "")
            .Replace(",", "")
            .Replace(" ", "-")
            .Replace("--", "-")
            .Replace("--", "-")
            .Trim('-');
}