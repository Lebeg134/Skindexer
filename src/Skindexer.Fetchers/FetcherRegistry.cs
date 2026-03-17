using Skindexer.Fetchers.Interfaces;
 
namespace Skindexer.Fetchers;
 
public class FetcherRegistry
{
    private readonly Dictionary<string, IGameFetcher> _fetchers;
 
    public FetcherRegistry(IEnumerable<IGameFetcher> fetchers)
    {
        _fetchers = fetchers.ToDictionary(f => f.GameId, StringComparer.OrdinalIgnoreCase);
    }
 
    /// <summary>All registered fetchers regardless of trigger type.</summary>
    public IReadOnlyCollection<IGameFetcher> All => _fetchers.Values;
 
    /// <summary>Only fetchers that run on an automated schedule.</summary>
    public IEnumerable<IScheduledFetcher> Scheduled =>
        _fetchers.Values.OfType<IScheduledFetcher>();
 
    /// <summary>Only fetchers that are triggered manually via the API.</summary>
    public IEnumerable<IManualFetcher> Manual =>
        _fetchers.Values.OfType<IManualFetcher>();
 
    /// <summary>Only fetchers that read from a local file or dataset.</summary>
    public IEnumerable<IFileFetcher> FileBased =>
        _fetchers.Values.OfType<IFileFetcher>();
 
    public IGameFetcher? Get(string gameId)
        => _fetchers.GetValueOrDefault(gameId);
 
    public bool Exists(string gameId)
        => _fetchers.ContainsKey(gameId);
}