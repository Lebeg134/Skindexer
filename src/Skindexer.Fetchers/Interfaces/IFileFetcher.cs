namespace Skindexer.Fetchers.Interfaces;
 
/// <summary>
/// Implement this for sources that read from a local file or dataset.
/// The scheduler ignores these — they are triggered manually or at startup.
///
/// Use this when:
/// - You have a local dataset (CSV, JSON) you want to seed the database with
/// - You want a dummy fetcher for local development without hitting live APIs
/// - You are importing historical data from a source like Kaggle
///
/// The data path is read from configuration via the fetcher's own options class,
/// e.g. KaggleFetcherOptions.CS2DataPath for the CS2 Kaggle fetcher.
///
/// Examples: Kaggle price history datasets, offline development seeds
/// </summary>
public interface IFileFetcher : IGameFetcher
{
    /// <summary>
    /// File extensions this fetcher can parse. e.g. [".csv", ".json"]
    /// Used for validation when a file path is configured.
    /// </summary>
    string[] SupportedExtensions { get; }
}