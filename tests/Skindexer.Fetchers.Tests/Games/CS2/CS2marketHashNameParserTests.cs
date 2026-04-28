using Skindexer.Fetchers.Games.CS2.SlugHelpers;

namespace Skindexer.Fetchers.Tests.Games.CS2;

public class CS2MarketHashNameParserTests
{
    #region Test Data Builders

    private static bool Parse(
        string marketHashName,
        out string weapon,
        out string skinName,
        out string wear,
        out bool statTrak,
        out bool souvenir)
        => CS2MarketHashNameParser.TryParse(
            marketHashName,
            out weapon,
            out skinName,
            out wear,
            out statTrak,
            out souvenir);

    /// <summary>
    /// Calls TryParse and asserts it returns true, then returns the out values
    /// as a tuple for further assertions. Fails the test immediately if parsing fails.
    /// </summary>
    private static (string Weapon, string SkinName, string Wear, bool StatTrak, bool Souvenir)
        ParseSuccess(string marketHashName)
    {
        var result = Parse(marketHashName,
            out var weapon, out var skinName, out var wear,
            out var statTrak, out var souvenir);

        Assert.True(result, $"Expected TryParse to return true for '{marketHashName}'");
        return (weapon, skinName, wear, statTrak, souvenir);
    }

    #endregion

    #region Guard Clauses

    [Fact]
    public void TryParse_WithNullInput_ReturnsFalse()
    {
        var result = Parse(null!, out _, out _, out _, out _, out _);

        Assert.False(result);
    }

    [Fact]
    public void TryParse_WithEmptyString_ReturnsFalse()
    {
        var input = string.Empty;

        var result = Parse(input, out _, out _, out _, out _, out _);

        Assert.False(result);
    }

    [Fact]
    public void TryParse_WithWhitespaceString_ReturnsFalse()
    {
        var input = "   ";

        var result = Parse(input, out _, out _, out _, out _, out _);

        Assert.False(result);
    }

    #endregion

    #region Returns False — Non-Parseable Items

    [Fact]
    public void TryParse_WithNoPipe_ReturnsFalse()
    {
        // Keys, cases, and other items with no " | " separator
        var input = "Operation Broken Fang Case";

        var result = Parse(input, out _, out _, out _, out _, out _);

        Assert.False(result);
    }

    [Fact]
    public void TryParse_WithPipeButNoWear_ReturnsFalse()
    {
        // Agents have a pipe but no wear suffix
        var input = "Sir Bloody Miami Darryl | The Professionals";

        var result = Parse(input, out _, out _, out _, out _, out _);

        Assert.False(result);
    }

    [Fact]
    public void TryParse_WithStickerFormat_ReturnsFalse()
    {
        // Stickers follow "Sticker | Name | Event" — no wear suffix
        var input = "Sticker | Fnatic | Katowice 2015";

        var result = Parse(input, out _, out _, out _, out _, out _);

        Assert.False(result);
    }

    [Fact]
    public void TryParse_WithBareKnifeNoSkin_ReturnsFalse()
    {
        // A knife with no skin variant has no pipe at all
        var input = "★ Karambit";

        var result = Parse(input, out _, out _, out _, out _, out _);

        Assert.False(result);
    }

    [Fact]
    public void TryParse_WithMusicKit_ReturnsFalse()
    {
        var input = "Music Kit | Darude, Moments";

        var result = Parse(input, out _, out _, out _, out _, out _);

        Assert.False(result);
    }

    #endregion

    #region Correctness — Plain Weapon Skins

    [Fact]
    public void TryParse_WithPlainWeaponSkin_ParsesWeaponAndSkinCorrectly()
    {
        var input = "AK-47 | Redline (Field-Tested)";

        var (weapon, skinName, _, _, _) = ParseSuccess(input);

        Assert.Equal("AK-47", weapon);
        Assert.Equal("Redline", skinName);
    }

    [Fact]
    public void TryParse_WithPlainWeaponSkin_ParsesWearCorrectly()
    {
        var input = "AK-47 | Redline (Field-Tested)";

        var (_, _, wear, _, _) = ParseSuccess(input);

        Assert.Equal("Field-Tested", wear);
    }

    [Fact]
    public void TryParse_WithPlainWeaponSkin_StatTrakAndSouvenirAreFalse()
    {
        var input = "AK-47 | Redline (Field-Tested)";

        var (_, _, _, statTrak, souvenir) = ParseSuccess(input);

        Assert.False(statTrak);
        Assert.False(souvenir);
    }

    [Fact]
    public void TryParse_WithMultiWordSkinName_ParsesSkinNameCorrectly()
    {
        var input = "M4A1-S | Hyper Beast (Battle-Scarred)";

        var (weapon, skinName, _, _, _) = ParseSuccess(input);

        Assert.Equal("M4A1-S", weapon);
        Assert.Equal("Hyper Beast", skinName);
    }

    [Fact]
    public void TryParse_WithKnifeSkin_ParsesCorrectly()
    {
        // Knife skins have the ★ prefix before the weapon name
        var input = "★ Karambit | Slaughter (Factory New)";

        var (weapon, skinName, wear, _, _) = ParseSuccess(input);

        Assert.Equal("★ Karambit", weapon);
        Assert.Equal("Slaughter", skinName);
        Assert.Equal("Factory New", wear);
    }

    #endregion

    #region Correctness — All Wear Values

    [Theory]
    [InlineData("AK-47 | Redline (Factory New)",     "Factory New")]
    [InlineData("AK-47 | Redline (Minimal Wear)",    "Minimal Wear")]
    [InlineData("AK-47 | Redline (Field-Tested)",    "Field-Tested")]
    [InlineData("AK-47 | Redline (Well-Worn)",       "Well-Worn")]
    [InlineData("AK-47 | Redline (Battle-Scarred)",  "Battle-Scarred")]
    public void TryParse_WithEachWearValue_ParsesWearCorrectly(string input, string expectedWear)
    {
        var (_, _, wear, _, _) = ParseSuccess(input);

        Assert.Equal(expectedWear, wear);
    }

    #endregion

    #region Correctness — StatTrak

    [Fact]
    public void TryParse_WithStatTrakPrefix_DetectsStatTrak()
    {
        var input = "StatTrak™ AK-47 | Redline (Minimal Wear)";

        var (_, _, _, statTrak, _) = ParseSuccess(input);

        Assert.True(statTrak);
    }

    [Fact]
    public void TryParse_WithStatTrakPrefix_SouvenirIsFalse()
    {
        var input = "StatTrak™ AK-47 | Redline (Minimal Wear)";

        var (_, _, _, _, souvenir) = ParseSuccess(input);

        Assert.False(souvenir);
    }

    [Fact]
    public void TryParse_WithStatTrakPrefix_StripsPrefix_WeaponIsCorrect()
    {
        var input = "StatTrak™ AK-47 | Redline (Minimal Wear)";

        var (weapon, _, _, _, _) = ParseSuccess(input);

        // "StatTrak™ " must be fully stripped — weapon must not contain it
        Assert.Equal("AK-47", weapon);
    }

    [Fact]
    public void TryParse_WithStatTrakKnife_ParsesCorrectly()
    {
        var input = "StatTrak™ ★ Karambit | Slaughter (Factory New)";

        var (weapon, skinName, wear, statTrak, souvenir) = ParseSuccess(input);

        Assert.Equal("★ Karambit", weapon);
        Assert.Equal("Slaughter", skinName);
        Assert.Equal("Factory New", wear);
        Assert.True(statTrak);
        Assert.False(souvenir);
    }

    #endregion

    #region Correctness — Souvenir

    [Fact]
    public void TryParse_WithSouvenirPrefix_DetectsSouvenir()
    {
        var input = "Souvenir AK-47 | Redline (Factory New)";

        var (_, _, _, _, souvenir) = ParseSuccess(input);

        Assert.True(souvenir);
    }

    [Fact]
    public void TryParse_WithSouvenirPrefix_StatTrakIsFalse()
    {
        var input = "Souvenir AK-47 | Redline (Factory New)";

        var (_, _, _, statTrak, _) = ParseSuccess(input);

        Assert.False(statTrak);
    }

    [Fact]
    public void TryParse_WithSouvenirPrefix_StripsPrefix_WeaponIsCorrect()
    {
        var input = "Souvenir AK-47 | Redline (Factory New)";

        var (weapon, _, _, _, _) = ParseSuccess(input);

        Assert.Equal("AK-47", weapon);
    }

    #endregion

    #region Correctness — Return Value

    [Fact]
    public void TryParse_WithValidWeaponSkin_ReturnsTrue()
    {
        var input = "AK-47 | Redline (Field-Tested)";

        var result = Parse(input, out _, out _, out _, out _, out _);

        Assert.True(result);
    }

    #endregion
}