using Cronos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Skindexer.Fetchers;
using Skindexer.Fetchers.Interfaces;

namespace Skindexer.Scheduler;

public class FetchScheduler : BackgroundService
{
    private readonly FetcherRegistry _registry;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<FetchScheduler> _logger;
    private readonly SchedulerOptions _options;

    // Tracks next scheduled run per fetcher, keyed by FetcherId
    private readonly Dictionary<string, DateTime> _nextRun = new();

    public FetchScheduler(
        FetcherRegistry registry,
        IServiceScopeFactory scopeFactory,
        ILogger<FetchScheduler> logger,
        IOptions<SchedulerOptions> options)
    {
        _registry = registry;
        _scopeFactory = scopeFactory;
        _logger = logger;
        _options = options.Value;
    }

    private CronExpression GetCron(IScheduledFetcher fetcher)
    {
        var expression = _options.Schedules.TryGetValue(fetcher.FetcherId, out var override_)
            ? override_
            : fetcher.DefaultCronExpression;

        return CronExpression.Parse(expression);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "FetchScheduler started. Scheduled fetchers: [{Scheduled}]",
            string.Join(", ", _registry.Scheduled.Select(f => f.FetcherId)));

        // Seed next run times from cron
        var now = DateTime.UtcNow;
        foreach (var fetcher in _registry.Scheduled)
        {
            var next = GetCron(fetcher).GetNextOccurrence(now, TimeZoneInfo.Utc);
            if (next.HasValue)
                _nextRun[fetcher.FetcherId] = next.Value;
        }

        // Optionally run all fetchers immediately on startup
        if (_options.FetchOnStartup)
        {
            _logger.LogInformation("FetchOnStartup enabled — running all fetchers now.");
            foreach (var fetcher in _registry.Scheduled)
                _ = RunFetcherAsync(fetcher, stoppingToken);
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            now = DateTime.UtcNow;

            foreach (var fetcher in _registry.Scheduled)
            {
                if (!_nextRun.TryGetValue(fetcher.FetcherId, out var next) || now < next)
                    continue;

                _ = RunFetcherAsync(fetcher, stoppingToken);

                // Schedule next occurrence
                var nextOccurrence = GetCron(fetcher).GetNextOccurrence(now, TimeZoneInfo.Utc);
                if (nextOccurrence.HasValue)
                    _nextRun[fetcher.FetcherId] = nextOccurrence.Value;
            }

            // Check every 30 seconds
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

        await using var scope = _scopeFactory.CreateAsyncScope();
        var persister = scope.ServiceProvider.GetRequiredService<IFetchResultPersister>();

        await persister.PersistAsync(result, ct);

        _logger.LogInformation(
            "Fetcher {FetcherId} completed. Items: {ItemCount}, Prices: {PriceCount}",
            fetcher.FetcherId, result.Items.Count, result.Prices.Count);
    }
}