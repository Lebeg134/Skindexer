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
/// Registration convention:
///   Every implementation must expose a static descriptor:
///
///     public static readonly FetcherDescriptor Descriptor = new()
///     {
///         FetcherId = "game-source",
///         Register  = (services, configuration) => { ... }
///     };
///
///   The descriptor must be added to the _descriptors array in FetcherServiceExtensions.
///   See docs/adding-a-fetcher.md for a full walkthrough. //TODO
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

    /// <summary>
    /// Indicates whether this fetcher is the authoritative source for item metadata.
    /// 
    /// When true  — the fetcher may overwrite existing item metadata on every run (e.g. ByMykel,
    ///              which provides the full item catalog including MinFloat, MaxFloat, collections
    ///              and rarities).
    /// When false — the fetcher may only insert genuinely new items; existing metadata is never
    ///              overwritten. Use this for price-only fetchers (e.g. cs2.sh) whose item records
    ///              are sparse and would otherwise clobber rich metadata from an authoritative source.
    ///
    /// Most fetchers should return false. Only set this to true if the fetcher produces
    /// complete, trusted item metadata that should always win over whatever is in the database.
    /// </summary>
    bool IsAuthoritativeItemSource { get; }
}