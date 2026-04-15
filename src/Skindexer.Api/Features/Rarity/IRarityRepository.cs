using Skindexer.Contracts.Responses;

namespace Skindexer.Api.Features.Rarity;

public interface IRarityRepository
{
    Task<IReadOnlyList<RarityResponse>> GetRaritiesByGameAsync(string gameId, CancellationToken ct = default);
}
