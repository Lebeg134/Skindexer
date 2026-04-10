namespace Skindexer.Fetchers.Games.CS2.SlugHelpers;

public static class CS2KaggleSlugHelper
{
    /// <summary>
    /// Produces a canonical slug for a CS2 skin wear variant from Kaggle data parts.
    /// e.g. weapon: "CZ75-Auto", skinName: "Victoria", wear: "Field-Tested", statTrak: false
    ///      → "cz75-auto-victoria-field-tested"
    /// e.g. weapon: "AK-47", skinName: "Redline", wear: "Minimal Wear", statTrak: true
    ///      → "ak-47-redline-stattrak-minimal-wear"
    /// </summary>
    public static string BuildPriceSlug(string weapon, string skinName, string wear, bool statTrak, bool souvenir)
    {
        var wearSlug = CS2WearHelper.WearSlugs.GetValueOrDefault(wear, Slugify(wear));

        var parts = new List<string>
        {
            Slugify(weapon),
            Slugify(skinName),
        };

        if (statTrak)
            parts.Add("stattrak");
        
        if (souvenir)
            parts.Add("souvenir");

        parts.Add(wearSlug);

        return string.Join("-", parts);
    }

    /// <summary>
    /// Produces the base item slug (no wear suffix) matching what CS2ByMykelSlugHelper
    /// would generate for the same item — used to look up the SkinItem FK.
    /// e.g. weapon: "AK-47", skinName: "Redline" → "ak-47-redline"
    /// </summary>
    public static string BuildBaseSlug(string weapon, string skinName) =>
        $"{Slugify(weapon)}-{Slugify(skinName)}";

    private static string Slugify(string value) =>
        value
            .ToLowerInvariant()
            .Replace("★", "")
            .Replace("™", "")
            .Replace("|", "")
            .Replace("'", "")
            .Replace(".", "")
            .Replace(" ", "-")
            .Replace("--", "-")
            .Trim('-');
}
