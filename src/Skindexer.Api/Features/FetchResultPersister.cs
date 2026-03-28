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

            await _prices.InsertPricesAsync(result.Prices, ct);

            _logger.LogInformation(
                "Inserted {Count} prices for {GameId}",
                result.Prices.Count, result.GameId);
        }
    }
}