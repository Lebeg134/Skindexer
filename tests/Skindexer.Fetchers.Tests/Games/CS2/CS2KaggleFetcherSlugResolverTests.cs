using Skindexer.Fetchers.Games.CS2.Fetchers;
using Skindexer.Fetchers.Games.CS2.Fetchers.KaggleFetcher;

namespace Skindexer.Fetchers.Tests.Games.CS2;

public class CS2KaggleSlugResolverTests
{
    #region Test Data Builders

    private static List<(string Slug, Guid VariantId)> Resolve(
        string slug,
        string weapon,
        string skinName,
        string wear,
        bool statTrak,
        bool souvenir,
        IReadOnlyDictionary<string, Guid>? slugMap = null,
        List<string>? warnings = null)
    {
        var context = new CS2KaggleFetchContext
        {
            VariantSlugMap = slugMap ?? new Dictionary<string, Guid>(),
        };

        return CS2KagglePriceFetcher.ResolveTargetSlugs(
            slug, weapon, skinName, wear, statTrak, souvenir,
            context, warnings ?? []);
    }

    private static IReadOnlyDictionary<string, Guid> SlugMap(params string[] slugs)
        => slugs.ToDictionary(s => s, _ => Guid.NewGuid());

    private static IReadOnlyDictionary<string, Guid> SlugMap(params (string Slug, Guid Id)[] entries)
        => entries.ToDictionary(e => e.Slug, e => e.Id);

    #endregion

    #region Empty Map Passthrough

    [Fact]
    public void ResolveTargetSlugs_EmptyMap_ReturnsSingleEntryWithGuidEmpty()
    {
        var results = Resolve(
            slug: "ak-47-redline-field-tested",
            weapon: "AK-47", skinName: "Redline",
            wear: "Field-Tested", statTrak: false, souvenir: false);

        var (slug, variantId) = Assert.Single(results);
        Assert.Equal("ak-47-redline-field-tested", slug);
        Assert.Equal(Guid.Empty, variantId);
    }

    [Fact]
    public void ResolveTargetSlugs_EmptyMap_EmitsNoWarnings()
    {
        var warnings = new List<string>();

        Resolve(
            slug: "ak-47-redline-field-tested",
            weapon: "AK-47", skinName: "Redline",
            wear: "Field-Tested", statTrak: false, souvenir: false,
            warnings: warnings);

        Assert.Empty(warnings);
    }

    #endregion

    #region Direct Hit

    [Fact]
    public void ResolveTargetSlugs_DirectHit_ReturnsSingleEntryWithCorrectVariantId()
    {
        var variantId = Guid.NewGuid();
        var slugMap = SlugMap(("ak-47-redline-field-tested", variantId));

        var results = Resolve(
            slug: "ak-47-redline-field-tested",
            weapon: "AK-47", skinName: "Redline",
            wear: "Field-Tested", statTrak: false, souvenir: false,
            slugMap: slugMap);

        var (slug, id) = Assert.Single(results);
        Assert.Equal("ak-47-redline-field-tested", slug);
        Assert.Equal(variantId, id);
    }

    [Fact]
    public void ResolveTargetSlugs_DirectHit_EmitsNoWarnings()
    {
        var warnings = new List<string>();
        var slugMap = SlugMap("ak-47-redline-field-tested");

        Resolve(
            slug: "ak-47-redline-field-tested",
            weapon: "AK-47", skinName: "Redline",
            wear: "Field-Tested", statTrak: false, souvenir: false,
            slugMap: slugMap, warnings: warnings);

        Assert.Empty(warnings);
    }

    [Fact]
    public void ResolveTargetSlugs_StatTrakDirectHit_ReturnsSingleEntryWithCorrectVariantId()
    {
        var variantId = Guid.NewGuid();
        var slugMap = SlugMap(("awp-asiimov-stattrak-field-tested", variantId));

        var results = Resolve(
            slug: "awp-asiimov-stattrak-field-tested",
            weapon: "AWP", skinName: "Asiimov",
            wear: "Field-Tested", statTrak: true, souvenir: false,
            slugMap: slugMap);

        var (slug, id) = Assert.Single(results);
        Assert.Equal("awp-asiimov-stattrak-field-tested", slug);
        Assert.Equal(variantId, id);
    }

    #endregion

    #region Fan-Out

    [Fact]
    public void ResolveTargetSlugs_DopplerFanOut_ReturnsAllPhases()
    {
        // Doppler phases share the same base slug and wear but differ by paint index discriminator
        var phase1Id = Guid.NewGuid();
        var phase2Id = Guid.NewGuid();
        var phase3Id = Guid.NewGuid();
        var phase4Id = Guid.NewGuid();

        var slugMap = SlugMap(
            ("bayonet-gamma-doppler-568-minimal-wear", phase1Id),
            ("bayonet-gamma-doppler-569-minimal-wear", phase2Id),
            ("bayonet-gamma-doppler-570-minimal-wear", phase3Id),
            ("bayonet-gamma-doppler-571-minimal-wear", phase4Id),
            ("bayonet-gamma-doppler-factory-new", Guid.NewGuid()) // different wear — must not match
        );

        var results = Resolve(
            slug: "bayonet-gamma-doppler-minimal-wear",
            weapon: "Bayonet", skinName: "Gamma Doppler",
            wear: "Minimal Wear", statTrak: false, souvenir: false,
            slugMap: slugMap);

        Assert.Equal(4, results.Count);
        Assert.Contains(results, r => r.VariantId == phase1Id);
        Assert.Contains(results, r => r.VariantId == phase2Id);
        Assert.Contains(results, r => r.VariantId == phase3Id);
        Assert.Contains(results, r => r.VariantId == phase4Id);
    }

    [Fact]
    public void ResolveTargetSlugs_DopplerFanOut_EmitsFanOutWarning()
    {
        var warnings = new List<string>();
        var slugMap = SlugMap(
            "bayonet-gamma-doppler-568-minimal-wear",
            "bayonet-gamma-doppler-569-minimal-wear");

        Resolve(
            slug: "bayonet-gamma-doppler-minimal-wear",
            weapon: "Bayonet", skinName: "Gamma Doppler",
            wear: "Minimal Wear", statTrak: false, souvenir: false,
            slugMap: slugMap, warnings: warnings);

        Assert.Single(warnings);
        Assert.Contains("fan-out", warnings[0], StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ResolveTargetSlugs_StatTrakFanOut_OnlyMatchesStatTrakVariants()
    {
        var statTrakId = Guid.NewGuid();
        var nonStatTrakId = Guid.NewGuid();

        var slugMap = SlugMap(
            ("karambit-doppler-415-stattrak-minimal-wear", statTrakId),
            ("karambit-doppler-415-minimal-wear", nonStatTrakId));

        var results = Resolve(
            slug: "karambit-doppler-stattrak-minimal-wear",
            weapon: "Karambit", skinName: "Doppler",
            wear: "Minimal Wear", statTrak: true, souvenir: false,
            slugMap: slugMap);

        Assert.Single(results);
        Assert.Equal(statTrakId, results[0].VariantId);
    }

    #endregion

    #region No Match

    [Fact]
    public void ResolveTargetSlugs_NoMatch_ReturnsEmpty()
    {
        var slugMap = SlugMap("m4a4-howl-field-tested"); // unrelated entry

        var results = Resolve(
            slug: "ak-47-redline-field-tested",
            weapon: "AK-47", skinName: "Redline",
            wear: "Field-Tested", statTrak: false, souvenir: false,
            slugMap: slugMap);

        Assert.Empty(results);
    }

    [Fact]
    public void ResolveTargetSlugs_NoMatch_EmitsWarning()
    {
        var warnings = new List<string>();
        var slugMap = SlugMap("m4a4-howl-field-tested");

        Resolve(
            slug: "ak-47-redline-field-tested",
            weapon: "AK-47", skinName: "Redline",
            wear: "Field-Tested", statTrak: false, souvenir: false,
            slugMap: slugMap, warnings: warnings);

        Assert.Single(warnings);
        Assert.Contains("ak-47-redline-field-tested", warnings[0]);
    }

    #endregion
}