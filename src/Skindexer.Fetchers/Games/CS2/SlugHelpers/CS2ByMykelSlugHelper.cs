using System.Text.RegularExpressions;

namespace Skindexer.Fetchers.Games.CS2.SlugHelpers;

public static partial class CS2ByMykelSlugHelper
{
    private static readonly Regex WearPattern = MyRegex();

    /// <summary>
    /// Produces a canonical base slug from a ByMykel item name.
    /// Wear variants are NOT included — they belong on SkinPrice, not SkinItem.
    /// e.g. "AK-47 | Redline"                  → "ak-47-redline"
    ///      "Sticker | Fnatic | Katowice 2014"  → "sticker-fnatic-katowice-2014"
    ///      "★ Bayonet | Boreal Forest"         → "bayonet-boreal-forest"
    ///      "★ Karambit"                        → "karambit"
    /// </summary>
    public static string GenerateSlug(string name) => Slugify(name);

    private static string Slugify(string value) =>
        WearPattern.Replace(value, "") // strip wear parens only
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