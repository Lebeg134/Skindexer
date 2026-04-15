using Skindexer.Contracts.Responses;

namespace Skindexer.Api.Features.Rarity;

public interface IRarityGroupRepository
{
    Task<IReadOnlyList<RarityGroupResponse>> GetRarityGroupsByGameAsync(string gameId, CancellationToken ct = default);
}