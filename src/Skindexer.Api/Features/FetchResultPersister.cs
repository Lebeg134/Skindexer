using Microsoft.Extensions.Logging;
using Npgsql;
using Skindexer.Api.Features.Items;
using Skindexer.Api.Features.Prices;
using Skindexer.Contracts.Models;
using Skindexer.Fetchers;
using Skindexer.Fetchers.Interfaces;

namespace Skindexer.Api.Features;

public class FetchResultPersister : IFetchResultPersister
{
    private readonly IItemRepository _items;
    private readonly IPriceRepository _prices;
    private readonly ILogger<FetchResultPersister> _logger;

    public FetchResultPersister(
        IItemRepository items,
        IPriceRepository prices,
        ILogger<FetchResultPersister> logger)
    {
        _items = items;
        _prices = prices;
        _logger = logger;
    }

    public async Task PersistAsync(FetchResult result, CancellationToken ct = default)
    {
        if (result.Items.Count > 0)
        {
            _logger.LogInformation(
                "Persisting {Count} items for {GameId} from {Source}",
                result.Items.Count, result.GameId, result.Source);

            await _items.UpsertItemsAsync(result.Items, ct);

            _logger.LogInformation(
                "Upserted {Count} items for {GameId}",
                result.Items.Count, result.GameId);
        }

        if (result.Prices.Count > 0)
        {
            _logger.LogInformation(
                "Persisting {Count} prices for {GameId} from {Source}",
                result.Prices.Count, result.GameId, result.Source);

            var slugMap = await _items.GetSlugToItemIdMapAsync(result.GameId, ct);

            if (slugMap.Count == 0)
            {
                _logger.LogWarning(
                    "Slug map is empty for {GameId} — items may not have been seeded yet. Skipping price write.",
                    result.GameId);
                return;
            }

            var resolved = new List<SkinPrice>(result.Prices.Count);
            var unresolved = new List<string>();

            foreach (var price in result.Prices)
            {
                if (price.ItemId != Guid.Empty)
                {
                    resolved.Add(price);
                    continue;
                }

                if (slugMap.TryGetValue(price.Slug, out var itemId))
                {
                    resolved.Add(new SkinPrice
                    {
                        ItemId = itemId,
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
                    unresolved.Count,
                    result.GameId,
                    unresolved.Take(10));
            }

            if (resolved.Count == 0)
            {
                _logger.LogWarning(
                    "No resolvable prices for {GameId} after slug resolution. Nothing written.",
                    result.GameId);
                return;
            }

            await _prices.UpsertPricesAsync(resolved, ct);

            _logger.LogInformation(
                "Wrote {Count} prices for {GameId} ({Unresolved} dropped)",
                resolved.Count, result.GameId, unresolved.Count);
        }
    }
}