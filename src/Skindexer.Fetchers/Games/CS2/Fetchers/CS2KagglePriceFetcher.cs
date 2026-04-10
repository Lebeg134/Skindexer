// Skindexer.Fetchers/Games/CS2/Fetchers/CS2KagglePriceFetcher.cs

using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Skindexer.Contracts.Constants;
using Skindexer.Contracts.Models;
using Skindexer.Fetchers.Games.CS2.SlugHelpers;
using Skindexer.Fetchers.Interfaces;
using Skindexer.Fetchers.Options;

namespace Skindexer.Fetchers.Games.CS2.Fetchers;

/// <summary>
/// Fetches CS2 prices from the Kaggle Steam dataset.
/// https://www.kaggle.com/datasets/kieranpoc/counter-strike-market-sale-data
/// </summary>
public sealed class CS2KagglePriceFetcher : IFileFetcher
{
    public string FetcherId   => "cs2-kaggle-steam";
    public string DisplayName => "CS2 Kaggle Steam Prices";
    public string[] SupportedExtensions => [".csv"];

    private const string GameId    = "cs2";
    private const string ItemsDir  = "items";
    private const string NameTable = "name_conversion_table.csv";

    private readonly ILogger<CS2KagglePriceFetcher> _logger;
    private readonly string _dataPath;

    public CS2KagglePriceFetcher(
        IOptions<KaggleFetcherOptions> options,
        ILogger<CS2KagglePriceFetcher> logger)
    {
        _logger   = logger;
        _dataPath = options.Value.CS2DataPath;
    }

    public async Task<FetchResult> FetchAsync(CancellationToken cancellationToken = default)
    {
        // ── Validate directory structure ──────────────────────────────────────
        var itemsPath = Path.Combine(_dataPath, ItemsDir);

        if (!Directory.Exists(itemsPath))
            return FetchResult.Failure(GameId, Sources.KaggleSteam,
                $"Items directory not found: {itemsPath}");

        // ── Load name conversion table ────────────────────────────────────────
        var nameTablePath = Path.Combine(_dataPath, NameTable);
        var nameMap       = await LoadNameTableAsync(nameTablePath, cancellationToken);

        if (nameMap.Count == 0)
            _logger.LogWarning(
                "Name conversion table is empty or missing at {Path}. " +
                "Falling back to filename decoding for all items.", nameTablePath);

        // ── Stream each CSV ───────────────────────────────────────────────────
        var csvFiles = Directory.EnumerateFiles(itemsPath, "*.csv");
        var prices   = new List<SkinPrice>();
        var warnings = new List<string>();
        var fileCount = 0;

        foreach (var filePath in csvFiles)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var encodedName   = Path.GetFileNameWithoutExtension(filePath);
            var marketHashName = nameMap.TryGetValue(encodedName, out var mapped)
                ? mapped
                : Uri.UnescapeDataString(encodedName);

            var filePrices = await ParsePriceFileAsync(
                filePath, marketHashName, warnings, cancellationToken);

            prices.AddRange(filePrices);
            fileCount++;

            if (fileCount % 500 == 0)
                _logger.LogInformation("Processed {Count} files...", fileCount);
        }

        _logger.LogInformation(
            "Kaggle import complete. Files: {Files}, Prices: {Prices}, Warnings: {Warnings}",
            fileCount, prices.Count, warnings.Count);

        return warnings.Count > 0
            ? FetchResult.Partial(GameId, Sources.KaggleSteam, [], [], prices, warnings)
            : FetchResult.Success(GameId, Sources.KaggleSteam, [], [], prices);
    }

    // ── Name table ────────────────────────────────────────────────────────────

    private static async Task<Dictionary<string, string>> LoadNameTableAsync(
        string path, CancellationToken ct)
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (!File.Exists(path))
            return map;

        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
        };

        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read,
            FileShare.Read, bufferSize: 65536, useAsync: true);
        using var reader = new StreamReader(stream);
        using var csv    = new CsvReader(reader, config);

        await csv.ReadAsync();
        csv.ReadHeader();

        while (await csv.ReadAsync())
        {
            ct.ThrowIfCancellationRequested();
            var encoded = csv.GetField(0);
            var decoded = csv.GetField(1);
            if (!string.IsNullOrWhiteSpace(encoded) && !string.IsNullOrWhiteSpace(decoded))
                map[encoded] = decoded;
        }

        return map;
    }

    // ── Per-file price parsing ────────────────────────────────────────────────

    private async Task<List<SkinPrice>> ParsePriceFileAsync(
        string filePath,
        string marketHashName,
        List<string> warnings,
        CancellationToken ct)
    {
        var (wear, statTrak, souvenir) = CS2WearHelper.ParseMarketHashName(marketHashName);

        if (wear is null)
        {
            warnings.Add(
                $"Skipped '{marketHashName}': no wear suffix found. " +
                "Item may be a knife, sticker, or other non-wear skin.");
            return [];
        }

        // Parse "StatTrak™ AK-47 | Redline (Field-Tested)"
        // → weapon: "AK-47", skinName: "Redline"
        var (weapon, skinName) = ParseWeaponAndSkin(marketHashName, statTrak, souvenir);

        if (weapon is null || skinName is null)
        {
            warnings.Add($"Skipped '{marketHashName}': could not parse weapon/skin name.");
            return [];
        }

        var slug = CS2KaggleSlugHelper.BuildPriceSlug(weapon, skinName, wear, statTrak, souvenir);
        var prices = new List<SkinPrice>();

        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord  = true,
            MissingFieldFound = null, // unix timestamp header has a space, be lenient
        };

        try
        {
            await using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read,
                FileShare.Read, bufferSize: 65536, useAsync: true);
            using var reader = new StreamReader(stream);
            using var csv    = new CsvReader(reader, config);

            await csv.ReadAsync();
            csv.ReadHeader();

            while (await csv.ReadAsync())
            {
                ct.ThrowIfCancellationRequested();

                if (!csv.TryGetField<decimal>(0, out var price))
                    continue;
                if (!csv.TryGetField<int>(1, out var quantity))
                    continue;
                // column 2 is the date string — skip it
                if (!csv.TryGetField<long>(3, out var unixTs))
                    continue;

                prices.Add(new SkinPrice
                {
                    VariantId  = Guid.Empty,
                    Slug       = slug,
                    Source     = Sources.KaggleSteam,
                    PriceType  = PriceTypes.MedianDaily,
                    Price      = price,
                    Currency   = "USD",
                    Volume     = quantity,
                    RecordedAt = DateTimeOffset.FromUnixTimeSeconds(unixTs).UtcDateTime,
                });
            }
        }
        catch (Exception ex)
        {
            warnings.Add($"Failed to parse '{marketHashName}': {ex.Message}");
        }

        return prices;
    }

    // ── Market hash name parsing ──────────────────────────────────────────────

    /// <summary>
    /// Extracts weapon and skin name from a market hash name.
    /// "AK-47 | Redline (Field-Tested)"           → ("AK-47", "Redline")
    /// "StatTrak™ AK-47 | Redline (Minimal Wear)" → ("AK-47", "Redline")
    /// </summary>
    private static (string? Weapon, string? SkinName) ParseWeaponAndSkin(
        string name, bool statTrak, bool souvenir)
    {
        // Strip StatTrak™ prefix
        if (statTrak)
            name = name.Replace("StatTrak™", "", StringComparison.OrdinalIgnoreCase).Trim();

        // Strip "Souvenir" suffix
        if (souvenir)
            name = name.Replace("Souvenir", "", StringComparison.OrdinalIgnoreCase).Trim();
        
        // Strip wear suffix "(Field-Tested)" etc.
        var wearStart = name.LastIndexOf('(');
        if (wearStart > 0)
            name = name[..wearStart].Trim();

        // Split on " | "
        var parts = name.Split(" | ", 2, StringSplitOptions.TrimEntries);
        if (parts.Length != 2)
            return (null, null);

        return (parts[0], parts[1]);
    }
}