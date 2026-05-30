using Skindexer.Fetchers.Models;

namespace Skindexer.Api.Features.FetchRuns;

public interface IFetchRunRepository
{
    Task<Guid> StartRunAsync(PersistOptions options, CancellationToken ct = default);
    Task CompleteRunAsync(Guid runId, PersistCounts counts, CancellationToken ct = default);
    Task FailRunAsync(Guid runId, string? errorMessage, CancellationToken ct = default);
}