using System.Text.RegularExpressions;
using Skindexer.Fetchers.Games.CS2.SlugHelpers;

namespace Skindexer.Fetchers.Games.CS2.Fetchers.ByMykelItemFetcher;

public static partial class CS2ByMykelSlugHelper
{
    private static readonly Regex WearPattern = MyRegex();

    /// <summary>
    /// Produces a canonical base slug from a ByMykel item name.
    /// e.g. "AK-47 | Redline"  → "ak-47-redline"
    ///      "★ Karambit"        → "karambit"
    /// </summary>
    public static string GenerateSlug(string name) => Slugify(name);

    /// <summary>
    /// Produces a canonical variant slug from an item slug, wear, StatTrak and Souvenir flags.
    /// Delegates to CS2SlugBuilder for consistency across all fetchers.
    ///
    /// e.g. itemSlug: "ak-47-redline", wear: "Field-Tested", statTrak: false
    ///      → "ak-47-redline-field-tested"
    ///      itemSlug: "ak-47-redline", wear: "Minimal Wear", statTrak: true
    ///      → "ak-47-redline-stattrak-minimal-wear"
    /// </summary>
    public static string BuildVariantSlug(
        string itemSlug,
        string wear,
        bool statTrak,
        bool souvenir)
        => CS2SlugBuilder.BuildVariantSlug(itemSlug, wear, statTrak, souvenir);

    /// <summary>
    /// Slugifies a raw ByMykel item name, stripping wear suffixes, special
    /// characters, and normalising to kebab-case.
    /// Internal — used only for GenerateSlug (item-level, not variant-level).
    /// </summary>
    private static string Slugify(string value) =>
        WearPattern.Replace(value, "")
            .ToLowerInvariant()
            .Replace("★ ", "")
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

    [GeneratedRegex(@"\((Factory New|Minimal Wear|Field-Tested|Well-Worn|Battle-Scarred)\)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled, "en-US")]
    private static partial Regex MyRegex();
}