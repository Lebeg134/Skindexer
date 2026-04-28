using Skindexer.Contracts.Models;
 
namespace Skindexer.Fetchers.Interfaces;
 
/// <summary>
/// Base interface for all game fetchers.
/// Every source — regardless of how it is triggered — implements this.
/// 
/// To add a new source, implement one of the derived interfaces:
///   IScheduledFetcher  — automated polling on a timer
///   IManualFetcher     — triggered on demand via the API
///   IFileFetcher       — reads from a local file or dataset
///
/// See docs/adding-a-fetcher.md for a full walkthrough.
/// </summary>
public interface IGameFetcher
{
    /// <summary>
    /// Unique identifier for this fetcher. Lowercase, no spaces. e.g. "cs2-my-fetcher", "rust-manual"
    /// </summary>
    string FetcherId { get; }
 
    /// <summary>
    /// Human-readable name shown in API responses and logs.
    /// </summary>
    string DisplayName { get; }
 
    /// <summary>
    /// Fetch items and prices for this game from the configured source.
    /// Must never throw — return FetchResult.Failure on error instead.
    /// </summary>
    Task<FetchResult> FetchAsync(CancellationToken cancellationToken = default);
    bool IsAuthoritativeItemSource { get; } 
}