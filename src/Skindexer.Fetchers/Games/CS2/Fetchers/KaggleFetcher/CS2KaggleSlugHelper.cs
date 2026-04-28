using Skindexer.Fetchers.Games.CS2.SlugHelpers;

namespace Skindexer.Fetchers.Games.CS2.Fetchers.KaggleFetcher;

public static class CS2KaggleSlugHelper
{
    /// <summary>
    /// Produces a canonical variant slug from Kaggle CSV fields.
    /// Delegates to CS2SlugBuilder — Kaggle is just one of several sources
    /// that build slugs from raw weapon/skin/wear parts.
    ///
    /// e.g. weapon: "AK-47", skinName: "Redline", wear: "Minimal Wear", statTrak: true
    ///      → "ak-47-redline-stattrak-minimal-wear"
    /// </summary>
    public static string BuildPriceSlug(
        string weapon,
        string skinName,
        string wear,
        bool statTrak,
        bool souvenir)
        => CS2SlugBuilder.BuildVariantSlug(weapon, skinName, wear, statTrak, souvenir);

    /// <summary>
    /// Produces the base item slug (no wear suffix) used to look up the SkinItem FK.
    /// e.g. weapon: "AK-47", skinName: "Redline" → "ak-47-redline"
    /// </summary>
    public static string BuildBaseSlug(string weapon, string skinName) =>
        CS2SlugBuilder.BuildBaseSlug(weapon, skinName);
}