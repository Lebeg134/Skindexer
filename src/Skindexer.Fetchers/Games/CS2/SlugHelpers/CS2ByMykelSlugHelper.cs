using System.Text.RegularExpressions;

namespace Skindexer.Fetchers.Games.CS2.SlugHelpers;

public static partial class CS2ByMykelSlugHelper
{
    private static readonly Regex WearPattern = MyRegex();

    /// <summary>
    /// Produces a canonical base slug from a ByMykel item name.
    /// e.g. "AK-47 | Redline"                  → "ak-47-redline"
    ///      "★ Karambit"                        → "karambit"
    /// </summary>
    public static string GenerateSlug(string name) => Slugify(name);

    /// <summary>
    /// Produces a canonical variant slug from an item slug, wear, StatTrak and Souvenir flags.
    /// Aligns with CS2KaggleSlugHelper.BuildPriceSlug format.
    /// e.g. itemSlug: "ak-47-redline", wear: "Field-Tested", statTrak: false → "ak-47-redline-field-tested"
    ///      itemSlug: "ak-47-redline", wear: "Minimal Wear",  statTrak: true  → "ak-47-redline-stattrak-minimal-wear"
    ///      itemSlug: "ak-47-redline", wear: "Factory New",   souvenir: true  → "ak-47-redline-souvenir-factory-new"
    /// </summary>
    public static string BuildVariantSlug(string itemSlug, string wear, bool statTrak, bool souvenir)
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