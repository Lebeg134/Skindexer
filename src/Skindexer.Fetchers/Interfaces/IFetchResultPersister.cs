using Skindexer.Contracts.Models;

namespace Skindexer.Fetchers.Interfaces;
 
/// <summary>
/// Persists a completed FetchResult to storage.
/// Implemented in Skindexer.Api, composed at the root in Program.cs.
/// The fetcher layer depends only on this interface — never on the Api or DbContext directly.
/// </summary>
public interface IFetchResultPersister
{
    Task PersistAsync(FetchResult result, CancellationToken ct = default);
}