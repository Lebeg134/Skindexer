using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Skindexer.Api.Features.Enrichment;
using Skindexer.Api.Features.Items;
using Skindexer.Api.Features.Prices;
using Skindexer.Api.Features.Variants;
using Skindexer.Contracts.Models;
using Skindexer.Fetchers.Interfaces;

namespace Skindexer.Api.Features;

public class FetchResultPersister : IFetchResultPersister
{
    private readonly IItemRepository _items;
    private readonly IVariantRepository _variants;
    private readonly IPriceRepository _prices;
    private readonly IEnumerable<IItemEnricher> _enrichers;
    private readonly ILogger<FetchResultPersister> _logger;

    public FetchResultPersister(
        IItemRepository items,
        IVariantRepository variants,
        IPriceRepository prices,
        IEnumerable<IItemEnricher> enrichers,
        ILogger<FetchResultPersister> logger)
    {
        _items = items;
        _variants = variants;
        _prices = prices;
        _enrichers = enrichers;
        _logger = logger;
    }

    public async Task PersistAsync(FetchResult result, CancellationToken ct = default)
    {
        await PersistItemsAsync(result, ct);
        await PersistVariantsAsync(result, ct);
        await PersistPricesAsync(result, ct);
        await EnrichAsync(result.GameId, ct);
    }

    // -------------------------------------------------------------------------
    // Items
    // -------------------------------------------------------------------------
    private async Task PersistItemsAsync(FetchResult result, CancellationToken ct)
    {
        if (result.Items.Count == 0) return;

        if (result.IsAuthoritativeItemSource)
        {
            // ByMykel — full upsert, metadata overwrite allowed
            await _items.UpsertItemsAsync(result.Items, ct);
            return;
        }

        // Non-authoritative source (cs2.sh etc.) — only insert genuinely new items,
        // never overwrite existing metadata
        var existingSlugMap = await _items.GetSlugToItemIdMapAsync(result.GameId, ct);
        var newItems = result.Items
            .Where(i => !existingSlugMap.ContainsKey(i.Slug))
            .ToList();

        if (newItems.Count > 0)
            await _items.UpsertItemsAsync(newItems, ct);
    }

    // -------------------------------------------------------------------------
    // Variants
    // -------------------------------------------------------------------------

    private async Task PersistVariantsAsync(FetchResult result, CancellationToken ct)
    {
        if (result.Variants.Count == 0) return;

        _logger.LogInformation(
            "Persisting {Count} variants for {GameId} from {Source}",
            result.Variants.Count, result.GameId, result.Source);

        var itemSlugMap = await _items.GetSlugToItemIdMapAsync(result.GameId, ct);

        var remapped = new List<SkinVariant>(result.Variants.Count);
        var unresolved = new List<string>();

        foreach (var variant in result.Variants)
        {
            var parentItem = result.Items.FirstOrDefault(i => i.Id == variant.ItemId);
            if (parentItem is null || !itemSlugMap.TryGetValue(parentItem.Slug, out var dbItemId))
            {
                unresolved.Add(variant.Slug);
                continue;
            }

            remapped.Add(new SkinVariant
            {
                Id = variant.Id,
                ItemId = dbItemId,
                GameId = variant.GameId,
                Slug = variant.Slug,
                Metadata = variant.Metadata,
            });
        }

        if (unresolved.Count > 0)
        {
            _logger.LogWarning(
                "Could not resolve parent item for {UnresolvedCount} variant(s) for {GameId} — dropping them. First 10: {Slugs}",
                unresolved.Count, result.GameId, unresolved.Take(10));
        }

        if (remapped.Count > 0)
        {
            await _variants.UpsertVariantsAsync(remapped, ct);

            _logger.LogInformation(
                "Upserted {Count} variants for {GameId} ({Unresolved} dropped)",
                remapped.Count, result.GameId, unresolved.Count);
        }
    }

    // -------------------------------------------------------------------------
    // Prices
    // -------------------------------------------------------------------------

    private async Task PersistPricesAsync(FetchResult result, CancellationToken ct)
    {
        if (result.Prices.Count == 0) return;

        _logger.LogInformation(
            "Persisting {Count} prices for {GameId} from {Source}",
            result.Prices.Count, result.GameId, result.Source);

        var slugMap = await _variants.GetSlugToVariantIdMapAsync(result.GameId, ct);

        if (slugMap.Count == 0)
        {
            _logger.LogWarning(
                "Variant slug map is empty for {GameId} — variants may not have been seeded yet. Skipping price write.",
                result.GameId);
            return;
        }

        var resolved = new List<SkinPrice>(result.Prices.Count);
        var unresolved = new List<string>();

        foreach (var price in result.Prices)
        {
            if (slugMap.TryGetValue(price.Slug, out var variantId))
            {
                resolved.Add(new SkinPrice
                {
                    VariantId = variantId,
                    GameId = price.GameId,
                    Slug = price.Slug,
                    Source = price.Source,
                    PriceType = price.PriceType,
                    Price = price.Price,
                    Currency = price.Currency,
                    Volume = price.Volume,
                    RecordedAt = price.RecordedAt
                });
            }
            else
            {
                unresolved.Add(price.Slug);
            }
        }

        if (unresolved.Count > 0)
        {
            _logger.LogWarning(
                "Could not resolve {UnresolvedCount} price slug(s) for {GameId} — dropping them. First 10: {Slugs}",
                unresolved.Count, result.GameId, unresolved.Take(10));
        }

        if (resolved.Count == 0)
        {
            _logger.LogWarning(
                "No resolvable prices for {GameId} after slug resolution. Nothing written.",
                result.GameId);
            return;
        }

        await WritePricesInBatchesAsync(result.GameId, resolved, ct);

        _logger.LogInformation(
            "Wrote {Count} prices for {GameId} ({Unresolved} dropped)",
            resolved.Count, result.GameId, unresolved.Count);
    }

    private async Task WritePricesInBatchesAsync(
        string gameId,
        List<SkinPrice> prices,
        CancellationToken ct)
    {
        const int batchSize = 50_000;

        var totalBatches = (int)Math.Ceiling(prices.Count / (double)batchSize);
        var batchIndex = 0;
        var timeLog = new List<long>();

        foreach (var batch in prices.Chunk(batchSize))
        {
            batchIndex++;
            var sw = Stopwatch.StartNew();
            await _prices.UpsertPricesAsync(batch, ct);
            sw.Stop();

            timeLog.Add(sw.ElapsedMilliseconds);
            var estimateTs = TimeSpan.FromMilliseconds(timeLog.Average() * (totalBatches - batchIndex));
            var finishAt = DateTime.UtcNow.Add(estimateTs);
            var estimateStr = estimateTs.TotalHours >= 1
                ? estimateTs.ToString(@"h\h\ m\m\ s\s")
                : estimateTs.TotalMinutes >= 1
                    ? estimateTs.ToString(@"m\m\ s\s")
                    : estimateTs.ToString(@"s\s");

            _logger.LogInformation(
                "Price import progress — batch {Current}/{Total} complete ({Prices} prices, {Ms}ms) ETA: {FinishAt:HH:mm:ss} UTC, remaining: {Estimate}",
                batchIndex, totalBatches, batch.Length, sw.ElapsedMilliseconds, finishAt, estimateStr);
        }
    }

    // -------------------------------------------------------------------------
    // Enrichment
    // -------------------------------------------------------------------------

    private async Task EnrichAsync(string gameId, CancellationToken ct)
    {
        var enrichers = _enrichers.Where(e => e.GameId == gameId).ToList();
        if (enrichers.Count == 0) return;

        _logger.LogInformation("Running {Count} enricher(s) for {GameId}", enrichers.Count, gameId);

        foreach (var enricher in enrichers)
            await enricher.EnrichAsync(ct);
    }
}