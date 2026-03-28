using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Skindexer.Fetchers;
using Skindexer.Fetchers.Interfaces;

namespace Skindexer.Scheduler;

public class FetchScheduler : BackgroundService
{
    private readonly FetcherRegistry _registry;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<FetchScheduler> _logger;

    // Tracks when each scheduled fetcher last ran, keyed by FetcherId
    private readonly Dictionary<string, DateTime> _lastRun = new();

    public FetchScheduler(
        FetcherRegistry registry,
        IServiceScopeFactory scopeFactory,
        ILogger<FetchScheduler> logger)
    {
        _registry = registry;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "FetchScheduler started. Scheduled fetchers: [{Scheduled}]",
            string.Join(", ", _registry.Scheduled.Select(f => f.FetcherId)));

        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTime.UtcNow;

            foreach (var fetcher in _registry.Scheduled)
            {
                var isDue = !_lastRun.TryGetValue(fetcher.FetcherId, out var last)
                    || (now - last) >= fetcher.PollingInterval;

                if (!isDue) continue;

                // Fire and forget per fetcher — one failing doesn't block others
                _ = RunFetcherAsync(fetcher, stoppingToken);
                _lastRun[fetcher.FetcherId] = now;
            }

            // Check every 30 seconds whether any fetcher is due
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }

    // Called by the scheduler loop for IScheduledFetcher
    // TODO: expose publicly so the API can call this for IManualFetcher triggers
    internal async Task RunFetcherAsync(IGameFetcher fetcher, CancellationToken ct)
    {
        _logger.LogInformation("Running fetcher {FetcherId}", fetcher.FetcherId);

        var result = await fetcher.FetchAsync(ct);

        if (!result.IsSuccess)
        {
            _logger.LogWarning("Fetcher {FetcherId} failed: {Error}",
                fetcher.FetcherId, result.ErrorMessage);
            return;
        }

        if (result.Warnings.Any())
            _logger.LogWarning("Fetcher {FetcherId} completed with warnings: {Warnings}",
                fetcher.FetcherId, string.Join(", ", result.Warnings));

        // Create a fresh scope per fetch run — persister wraps DbContext, which is scoped
        await using var scope = _scopeFactory.CreateAsyncScope();
        var persister = scope.ServiceProvider.GetRequiredService<IFetchResultPersister>();

        await persister.PersistAsync(result, ct);

        _logger.LogInformation(
            "Fetcher {FetcherId} completed. Items: {ItemCount}, Prices: {PriceCount}",
            fetcher.FetcherId, result.Items.Count, result.Prices.Count);
    }
}