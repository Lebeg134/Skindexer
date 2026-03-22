using Skindexer.Contracts.Responses;

namespace Skindexer.Api.Features.Collections;

public interface ICollectionRepository
{
    Task<IReadOnlyList<CollectionResponse>> GetCollectionsByGameAsync(string gameId, CancellationToken ct = default);
}