namespace Skindexer.Fetchers.Interfaces;
 
/// <summary>
/// Implement this for sources that should be polled automatically on a timer.
/// The scheduler calls FetchAsync based on PollingInterval.
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
    /// How often this fetcher should be polled.
    /// Be respectful of upstream API rate limits.
    /// The scheduler uses this to determine when FetchAsync is next due.
    /// </summary>
    TimeSpan PollingInterval { get; }
}