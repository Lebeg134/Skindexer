using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Skindexer.Fetchers;
using Skindexer.Fetchers.Interfaces;

namespace Skindexer.Scheduler;

public class FetchScheduler : BackgroundService
{
    private readonly FetcherRegistry _registry;
    private readonly ILogger<FetchScheduler> _logger;

    // Tracks when each scheduled fetcher last ran, keyed by GameId
    private readonly Dictionary<string, DateTime> _lastRun = new();

    public FetchScheduler(FetcherRegistry registry, ILogger<FetchScheduler> logger)
    {
        _registry = registry;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "FetchScheduler started. Scheduled fetchers: [{Scheduled}]",
            string.Join(", ", _registry.Scheduled.Select(f => f.GameId)));

        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTime.UtcNow;

            foreach (var fetcher in _registry.Scheduled)
            {
                var isDue = !_lastRun.TryGetValue(fetcher.GameId, out var last)
                    || (now - last) >= fetcher.PollingInterval;

                if (!isDue) continue;

                // Fire and forget per fetcher — one failing doesn't block others
                _ = RunFetcherAsync(fetcher, stoppingToken);
                _lastRun[fetcher.GameId] = now;
            }

            // Check every 30 seconds whether any fetcher is due
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }

    // Called by the scheduler loop for IScheduledFetcher
    // TODO: expose publicly so the API can call this for IManualFetcher triggers
    internal async Task RunFetcherAsync(IGameFetcher fetcher, CancellationToken ct)
    {
        _logger.LogInformation("Running fetcher for {Game}", fetcher.DisplayName);

        var result = await fetcher.FetchAsync(ct);

        if (!result.IsSuccess)
        {
            _logger.LogWarning("Fetcher for {Game} failed: {Error}",
                fetcher.DisplayName, result.ErrorMessage);
            return;
        }

        if (result.Warnings.Any())
            _logger.LogWarning("Fetcher for {Game} completed with warnings: {Warnings}",
                fetcher.DisplayName, string.Join(", ", result.Warnings));

        _logger.LogInformation(
            "Fetcher for {Game} completed. Items: {ItemCount}, Prices: {PriceCount}",
            fetcher.DisplayName, result.Items.Count, result.Prices.Count);

        // TODO: persist result.Items and result.Prices to PostgreSQL
    }
}