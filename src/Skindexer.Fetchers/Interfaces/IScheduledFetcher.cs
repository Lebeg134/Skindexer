namespace Skindexer.Fetchers.Interfaces;

/// <summary>
/// Implement this for sources that should be polled automatically on a timer.
/// The scheduler calls FetchAsync based on DefaultCronExpression.
///
/// Use this when:
/// - The source has a public API you can query programmatically
/// - Data updates frequently, and you want it kept fresh automatically
///
/// Examples: Steam Market or any game with a live price API
/// </summary>
public interface IScheduledFetcher : IGameFetcher
{
    /// <summary>
    /// Default cron expression defining when this fetcher runs.
    /// Uses standard 5-field cron syntax (minute hour day month weekday).
    /// Be respectful of upstream API rate limits.
    /// Can be overridden per-fetcher via Fetchers__Schedules__{FetcherId} config.
    /// </summary>
    string DefaultCronExpression { get; }
}